using System.Data;
using System.Data.SqlClient;
using MySql.Data.MySqlClient;
using Npgsql;
using AutoFixer.Models;
using System.Text.Json;

namespace AutoFixer.Integrations.Database
{
    public interface IDatabaseIntegrationService
    {
        Task<List<ErrorPattern>> ImportFromSqlServerAsync(string connectionString, string query, DateTime since);
        Task<List<ErrorPattern>> ImportFromPostgreSqlAsync(string connectionString, string query, DateTime since);
        Task<List<ErrorPattern>> ImportFromMySqlAsync(string connectionString, string query, DateTime since);
        Task<List<ErrorPattern>> ImportApplicationLogsAsync(string connectionString, DatabaseType dbType, DateTime since);
    }

    public class DatabaseIntegrationService : IDatabaseIntegrationService
    {
        private readonly ILogger<DatabaseIntegrationService> _logger;

        public DatabaseIntegrationService(ILogger<DatabaseIntegrationService> logger)
        {
            _logger = logger;
        }

        public async Task<List<ErrorPattern>> ImportFromSqlServerAsync(string connectionString, string query, DateTime since)
        {
            var errors = new List<DatabaseError>();
            
            using var connection = new SqlConnection(connectionString);
            await connection.OpenAsync();
            
            using var command = new SqlCommand(query, connection);
            command.Parameters.AddWithValue("@since", since);
            
            using var reader = await command.ExecuteReaderAsync();
            
            while (await reader.ReadAsync())
            {
                errors.Add(MapReaderToError(reader));
            }
            
            _logger.LogInformation("Imported {Count} errors from SQL Server", errors.Count);
            
            return ConvertToErrorPatterns(errors);
        }

        public async Task<List<ErrorPattern>> ImportFromPostgreSqlAsync(string connectionString, string query, DateTime since)
        {
            var errors = new List<DatabaseError>();
            
            using var connection = new NpgsqlConnection(connectionString);
            await connection.OpenAsync();
            
            using var command = new NpgsqlCommand(query, connection);
            command.Parameters.AddWithValue("@since", since);
            
            using var reader = await command.ExecuteReaderAsync();
            
            while (await reader.ReadAsync())
            {
                errors.Add(MapReaderToError(reader));
            }
            
            _logger.LogInformation("Imported {Count} errors from PostgreSQL", errors.Count);
            
            return ConvertToErrorPatterns(errors);
        }

        public async Task<List<ErrorPattern>> ImportFromMySqlAsync(string connectionString, string query, DateTime since)
        {
            var errors = new List<DatabaseError>();
            
            using var connection = new MySqlConnection(connectionString);
            await connection.OpenAsync();
            
            using var command = new MySqlCommand(query, connection);
            command.Parameters.AddWithValue("@since", since);
            
            using var reader = await command.ExecuteReaderAsync();
            
            while (await reader.ReadAsync())
            {
                errors.Add(MapReaderToError(reader));
            }
            
            _logger.LogInformation("Imported {Count} errors from MySQL", errors.Count);
            
            return ConvertToErrorPatterns(errors);
        }

        public async Task<List<ErrorPattern>> ImportApplicationLogsAsync(string connectionString, DatabaseType dbType, DateTime since)
        {
            var query = GetStandardLogQuery(dbType);
            
            return dbType switch
            {
                DatabaseType.SqlServer => await ImportFromSqlServerAsync(connectionString, query, since),
                DatabaseType.PostgreSql => await ImportFromPostgreSqlAsync(connectionString, query, since),
                DatabaseType.MySql => await ImportFromMySqlAsync(connectionString, query, since),
                _ => throw new NotSupportedException($"Database type {dbType} is not supported")
            };
        }

        private string GetStandardLogQuery(DatabaseType dbType)
        {
            return dbType switch
            {
                DatabaseType.SqlServer => @"
                    SELECT 
                        LogLevel,
                        Message,
                        Exception,
                        TimeStamp,
                        Application,
                        MachineName,
                        Logger,
                        CallSite,
                        Exception AS StackTrace
                    FROM ApplicationLogs 
                    WHERE TimeStamp >= @since 
                        AND LogLevel IN ('Error', 'Fatal', 'Warn')
                    ORDER BY TimeStamp DESC",
                
                DatabaseType.PostgreSql => @"
                    SELECT 
                        log_level as LogLevel,
                        message as Message,
                        exception as Exception,
                        timestamp as TimeStamp,
                        application as Application,
                        machine_name as MachineName,
                        logger as Logger,
                        call_site as CallSite,
                        exception as StackTrace
                    FROM application_logs 
                    WHERE timestamp >= @since 
                        AND log_level IN ('Error', 'Fatal', 'Warn')
                    ORDER BY timestamp DESC",
                
                DatabaseType.MySql => @"
                    SELECT 
                        LogLevel,
                        Message,
                        Exception,
                        TimeStamp,
                        Application,
                        MachineName,
                        Logger,
                        CallSite,
                        Exception AS StackTrace
                    FROM ApplicationLogs 
                    WHERE TimeStamp >= @since 
                        AND LogLevel IN ('Error', 'Fatal', 'Warn')
                    ORDER BY TimeStamp DESC",
                
                _ => throw new NotSupportedException($"Database type {dbType} is not supported")
            };
        }

        private DatabaseError MapReaderToError(IDataReader reader)
        {
            return new DatabaseError
            {
                LogLevel = GetStringValue(reader, "LogLevel"),
                Message = GetStringValue(reader, "Message"),
                Exception = GetStringValue(reader, "Exception"),
                TimeStamp = GetDateTimeValue(reader, "TimeStamp"),
                Application = GetStringValue(reader, "Application"),
                MachineName = GetStringValue(reader, "MachineName"),
                Logger = GetStringValue(reader, "Logger"),
                CallSite = GetStringValue(reader, "CallSite"),
                StackTrace = GetStringValue(reader, "StackTrace")
            };
        }

        private string GetStringValue(IDataReader reader, string columnName)
        {
            try
            {
                var ordinal = reader.GetOrdinal(columnName);
                return reader.IsDBNull(ordinal) ? string.Empty : reader.GetString(ordinal);
            }
            catch
            {
                return string.Empty;
            }
        }

        private DateTime GetDateTimeValue(IDataReader reader, string columnName)
        {
            try
            {
                var ordinal = reader.GetOrdinal(columnName);
                return reader.IsDBNull(ordinal) ? DateTime.MinValue : reader.GetDateTime(ordinal);
            }
            catch
            {
                return DateTime.MinValue;
            }
        }

        private List<ErrorPattern> ConvertToErrorPatterns(List<DatabaseError> errors)
        {
            var patterns = new List<ErrorPattern>();
            
            // Group errors by similar characteristics
            var errorGroups = errors.GroupBy(e => new 
            { 
                LogLevel = e.LogLevel,
                ExceptionType = ExtractExceptionType(e.Exception),
                Application = e.Application,
                Logger = e.Logger
            });

            foreach (var group in errorGroups)
            {
                var errorList = group.ToList();
                var first = errorList.OrderBy(e => e.TimeStamp).First();
                var last = errorList.OrderBy(e => e.TimeStamp).Last();

                var pattern = new ErrorPattern
                {
                    Id = Guid.NewGuid().ToString(),
                    Name = GeneratePatternName(first, errorList.Count),
                    Description = first.Message,
                    ErrorType = DetermineErrorType(first.Exception, first.Message),
                    Priority = DeterminePriority(errorList.Count, first.LogLevel),
                    Status = PatternStatus.Active,
                    Confidence = CalculateConfidence(errorList),
                    FirstOccurrence = first.TimeStamp,
                    LastOccurrence = last.TimeStamp,
                    OccurrenceCount = errorList.Count,
                    OccurrenceRate = CalculateOccurrenceRate(errorList),
                    AffectedServices = errorList.Select(e => e.Application).Distinct().ToList(),
                    UserImpact = EstimateUserImpact(errorList.Count, first.LogLevel),
                    TechnicalDetails = new TechnicalDetails
                    {
                        StackTrace = first.StackTrace,
                        ErrorCode = ExtractExceptionType(first.Exception),
                        ComponentsAffected = errorList.Select(e => e.Logger).Distinct().ToList(),
                        InfrastructureImpact = new InfrastructureImpact
                        {
                            CpuUsage = 0,
                            MemoryUsage = 0,
                            DiskUsage = 0,
                            NetworkLatency = 0
                        }
                    },
                    BusinessImpact = new BusinessImpact
                    {
                        RevenueImpact = EstimateRevenueImpact(errorList.Count, first.LogLevel),
                        CustomerSatisfactionScore = EstimateCSAT(errorList.Count),
                        ServiceLevelImpact = EstimateSLImpact(errorList.Count)
                    },
                    Tags = new List<string> { "database-import", first.Application, first.LogLevel, group.Key.ExceptionType }
                };

                patterns.Add(pattern);
            }

            return patterns;
        }

        private string ExtractExceptionType(string exception)
        {
            if (string.IsNullOrEmpty(exception)) return "Unknown";
            
            var lines = exception.Split('\n');
            var firstLine = lines[0];
            
            if (firstLine.Contains(':'))
            {
                return firstLine.Split(':')[0].Trim();
            }
            
            return firstLine.Length > 50 ? firstLine.Substring(0, 50) + "..." : firstLine;
        }

        private string GeneratePatternName(DatabaseError error, int count)
        {
            var exceptionType = ExtractExceptionType(error.Exception);
            return $"{exceptionType} in {error.Application} ({count} occurrences)";
        }

        private ErrorType DetermineErrorType(string exception, string message)
        {
            var combined = (exception + " " + message).ToLower();
            
            return combined switch
            {
                var c when c.Contains("timeout") || c.Contains("slow") => ErrorType.Performance,
                var c when c.Contains("connection") || c.Contains("network") => ErrorType.Infrastructure,
                var c when c.Contains("unauthorized") || c.Contains("forbidden") => ErrorType.Security,
                var c when c.Contains("validation") || c.Contains("invalid") => ErrorType.BusinessLogic,
                var c when c.Contains("null") || c.Contains("reference") => ErrorType.ApplicationLogic,
                var c when c.Contains("format") || c.Contains("parse") => ErrorType.DataQuality,
                var c when c.Contains("sql") || c.Contains("database") => ErrorType.DataAccess,
                _ => ErrorType.Unknown
            };
        }

        private PatternPriority DeterminePriority(int count, string logLevel)
        {
            return logLevel.ToLower() switch
            {
                "fatal" => PatternPriority.Critical,
                "error" when count > 10 => PatternPriority.Critical,
                "error" => PatternPriority.High,
                "warn" when count > 50 => PatternPriority.High,
                "warn" when count > 10 => PatternPriority.Medium,
                _ => PatternPriority.Low
            };
        }

        private double CalculateConfidence(List<DatabaseError> errors)
        {
            var count = errors.Count;
            var timeSpan = errors.Max(e => e.TimeStamp) - errors.Min(e => e.TimeStamp);
            var uniqueMessages = errors.Select(e => e.Message).Distinct().Count();
            
            var baseConfidence = Math.Min(0.9, count / 20.0 + 0.3);
            var consistencyBonus = uniqueMessages == 1 ? 0.2 : Math.Max(0, 0.2 - (uniqueMessages / count));
            var frequencyBonus = timeSpan.TotalHours > 0 && (count / timeSpan.TotalHours) > 1 ? 0.1 : 0;
            
            return Math.Min(0.98, baseConfidence + consistencyBonus + frequencyBonus);
        }

        private double CalculateOccurrenceRate(List<DatabaseError> errors)
        {
            if (errors.Count < 2) return errors.Count;
            
            var timeSpan = errors.Max(e => e.TimeStamp) - errors.Min(e => e.TimeStamp);
            return timeSpan.TotalHours > 0 ? errors.Count / timeSpan.TotalHours : errors.Count;
        }

        private int EstimateUserImpact(int errorCount, string logLevel)
        {
            var multiplier = logLevel.ToLower() switch
            {
                "fatal" => 5.0,
                "error" => 2.0,
                "warn" => 0.5,
                _ => 0.1
            };
            
            return (int)(errorCount * multiplier);
        }

        private double EstimateRevenueImpact(int errorCount, string logLevel)
        {
            var baseImpact = logLevel.ToLower() switch
            {
                "fatal" => 500.0,
                "error" => 100.0,
                "warn" => 25.0,
                _ => 5.0
            };
            
            return errorCount * baseImpact;
        }

        private double EstimateCSAT(int errorCount)
        {
            return Math.Max(1.0, 5.0 - (errorCount / 30.0));
        }

        private double EstimateSLImpact(int errorCount)
        {
            return Math.Min(100.0, errorCount * 0.3);
        }
    }

    public class DatabaseError
    {
        public string LogLevel { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public string Exception { get; set; } = string.Empty;
        public DateTime TimeStamp { get; set; }
        public string Application { get; set; } = string.Empty;
        public string MachineName { get; set; } = string.Empty;
        public string Logger { get; set; } = string.Empty;
        public string CallSite { get; set; } = string.Empty;
        public string StackTrace { get; set; } = string.Empty;
    }

    public enum DatabaseType
    {
        SqlServer,
        PostgreSql,
        MySql
    }
}