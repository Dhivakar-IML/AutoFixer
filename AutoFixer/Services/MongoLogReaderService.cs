using AutoFixer.Models;
using AutoFixer.Services.Interfaces;
using AutoFixer.Configuration;
using Microsoft.Extensions.Options;
using System.Text.RegularExpressions;

namespace AutoFixer.Services
{
    /// <summary>
    /// Service for reading and parsing MongoDB log files to extract error information
    /// </summary>
    public class MongoLogReaderService
    {
        private readonly MongoDbSettings _settings;
        private readonly ILogger<MongoLogReaderService> _logger;

        public MongoLogReaderService(
            IOptions<MongoDbSettings> settings,
            ILogger<MongoLogReaderService> logger)
        {
            _settings = settings.Value;
            _logger = logger;
        }

        /// <summary>
        /// Reads MongoDB log files from common locations and extracts errors
        /// </summary>
        public async Task<List<ErrorEntry>> ReadMongoLogsAsync(DateTime since, CancellationToken cancellationToken = default)
        {
            var errors = new List<ErrorEntry>();

            // Common MongoDB log file locations
            var logPaths = new[]
            {
                // Linux/Unix paths
                "/var/log/mongodb/mongod.log",
                "/var/log/mongodb/mongo.log",
                "/usr/local/var/log/mongodb/mongod.log",
                
                // Windows paths
                @"C:\Program Files\MongoDB\Server\7.0\log\mongod.log",
                @"C:\Program Files\MongoDB\Server\6.0\log\mongod.log",
                @"C:\Program Files\MongoDB\Server\5.0\log\mongod.log",
                @"C:\data\log\mongod.log",
                
                // Docker/Container paths
                "/data/db/mongod.log",
                
                // Custom path from settings (if available)
                _settings.LogPath
            };

            foreach (var logPath in logPaths.Where(p => !string.IsNullOrEmpty(p)))
            {
                if (File.Exists(logPath))
                {
                    _logger.LogInformation("Found MongoDB log file: {LogPath}", logPath);
                    var logErrors = await ParseMongoLogFileAsync(logPath, since, cancellationToken);
                    errors.AddRange(logErrors);
                    
                    _logger.LogInformation("Extracted {Count} errors from {LogPath}", logErrors.Count, logPath);
                    
                    // If we found a log file and got errors, break
                    if (logErrors.Count > 0) break;
                }
            }

            if (errors.Count == 0)
            {
                _logger.LogWarning("No MongoDB log files found or no errors extracted. Checked paths: {Paths}", 
                    string.Join(", ", logPaths.Where(p => !string.IsNullOrEmpty(p))));
            }

            return errors;
        }

        /// <summary>
        /// Parses a MongoDB log file and extracts error entries
        /// MongoDB log format: 2024-10-24T10:30:45.123+0000 I NETWORK  [listener] connection accepted...
        /// Severity levels: F (Fatal), E (Error), W (Warning), I (Info), D (Debug)
        /// </summary>
        private async Task<List<ErrorEntry>> ParseMongoLogFileAsync(
            string logPath, 
            DateTime since, 
            CancellationToken cancellationToken)
        {
            var errors = new List<ErrorEntry>();

            try
            {
                // Read log file in chunks to avoid memory issues with large files
                using var stream = new FileStream(logPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                using var reader = new StreamReader(stream);

                var lineCount = 0;
                var processedCount = 0;
                string? line;

                // MongoDB log entry regex pattern
                // Format: 2024-10-24T10:30:45.123+0000 E COMMAND  [conn123] command failed...
                var logPattern = new Regex(@"^(\d{4}-\d{2}-\d{2}T\d{2}:\d{2}:\d{2}\.\d{3}[+-]\d{4})\s+([FEWID])\s+(\w+)\s+\[(.*?)\]\s+(.+)$", 
                    RegexOptions.Compiled);

                while ((line = await reader.ReadLineAsync(cancellationToken)) != null)
                {
                    lineCount++;
                    
                    if (cancellationToken.IsCancellationRequested)
                        break;

                    // Parse the log line
                    var match = logPattern.Match(line);
                    if (match.Success)
                    {
                        var timestampStr = match.Groups[1].Value;
                        var severity = match.Groups[2].Value;
                        var component = match.Groups[3].Value;
                        var context = match.Groups[4].Value;
                        var message = match.Groups[5].Value;

                        // Only process error and warning level entries
                        if (severity == "E" || severity == "F" || severity == "W")
                        {
                            if (DateTime.TryParse(timestampStr, out var timestamp) && timestamp >= since)
                            {
                                var errorEntry = new ErrorEntry
                                {
                                    Timestamp = timestamp,
                                    Message = message,
                                    Source = $"MongoDB {component}",
                                    Severity = MapSeverity(severity),
                                    Context = new Dictionary<string, object>
                                    {
                                        { "Component", component },
                                        { "Context", context },
                                        { "LogFile", logPath },
                                        { "LineNumber", lineCount },
                                        { "SeverityCode", severity }
                                    }
                                };

                                // Extract exception type if present
                                if (message.Contains("exception", StringComparison.OrdinalIgnoreCase))
                                {
                                    errorEntry.ExceptionType = ExtractExceptionType(message);
                                }

                                // Extract connection info if present
                                if (context.Contains("conn") || message.Contains("connection"))
                                {
                                    errorEntry.Context["ConnectionInfo"] = context;
                                }

                                errors.Add(errorEntry);
                                processedCount++;
                            }
                        }
                    }
                    else if (IsErrorLine(line))
                    {
                        // Fallback parsing for non-standard log formats
                        var timestamp = ExtractTimestamp(line);
                        if (timestamp >= since)
                        {
                            errors.Add(new ErrorEntry
                            {
                                Timestamp = timestamp,
                                Message = line,
                                Source = "MongoDB Log",
                                Severity = GetSeverityFromLine(line),
                                Context = new Dictionary<string, object>
                                {
                                    { "LogFile", logPath },
                                    { "LineNumber", lineCount },
                                    { "RawLine", line }
                                }
                            });
                            processedCount++;
                        }
                    }

                    // Limit processing to avoid performance issues
                    if (processedCount >= 10000)
                    {
                        _logger.LogWarning("Reached processing limit of 10000 errors from {LogPath}", logPath);
                        break;
                    }
                }

                _logger.LogInformation("Processed {LineCount} lines, extracted {ErrorCount} errors from {LogPath}", 
                    lineCount, errors.Count, logPath);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to parse MongoDB log file: {LogPath}", logPath);
                
                // Add the parsing error as an entry
                errors.Add(new ErrorEntry
                {
                    Timestamp = DateTime.UtcNow,
                    Message = $"Failed to parse MongoDB log file: {ex.Message}",
                    ExceptionType = ex.GetType().Name,
                    Source = "MongoDB Log Parser",
                    Severity = ErrorSeverity.Warning,
                    Context = new Dictionary<string, object>
                    {
                        { "LogFile", logPath },
                        { "Error", ex.Message },
                        { "StackTrace", ex.StackTrace ?? "" }
                    }
                });
            }

            return errors;
        }

        /// <summary>
        /// Checks if a log line indicates an error
        /// </summary>
        private bool IsErrorLine(string line)
        {
            var errorKeywords = new[] 
            { 
                " E ", " F ", "ERROR", "FATAL", "FAILURE", "FAILED",
                "exception", "error:", "timeout", "refused", "denied",
                "cannot", "unable", "invalid", "unauthorized"
            };

            return errorKeywords.Any(keyword => 
                line.Contains(keyword, StringComparison.OrdinalIgnoreCase));
        }

        /// <summary>
        /// Maps MongoDB log severity codes to ErrorSeverity enum
        /// </summary>
        private ErrorSeverity MapSeverity(string severityCode)
        {
            return severityCode switch
            {
                "F" => ErrorSeverity.Emergency,  // Fatal
                "E" => ErrorSeverity.Critical,   // Error
                "W" => ErrorSeverity.Warning,    // Warning
                _ => ErrorSeverity.Info
            };
        }

        /// <summary>
        /// Extracts severity from log line content
        /// </summary>
        private ErrorSeverity GetSeverityFromLine(string line)
        {
            if (line.Contains(" F ") || line.Contains("FATAL")) 
                return ErrorSeverity.Emergency;
            if (line.Contains(" E ") || line.Contains("ERROR")) 
                return ErrorSeverity.Critical;
            if (line.Contains(" W ") || line.Contains("WARNING") || line.Contains("timeout")) 
                return ErrorSeverity.Warning;
            return ErrorSeverity.Info;
        }

        /// <summary>
        /// Extracts timestamp from log line
        /// </summary>
        private DateTime ExtractTimestamp(string line)
        {
            try
            {
                // Try to parse ISO 8601 timestamp at the beginning of the line
                // Format: 2024-10-24T10:30:45.123+0000 or 2024-10-24T10:30:45.123Z
                var timestampMatch = Regex.Match(line, @"^(\d{4}-\d{2}-\d{2}T\d{2}:\d{2}:\d{2}\.\d{3}[+-]\d{4}|\d{4}-\d{2}-\d{2}T\d{2}:\d{2}:\d{2}\.\d{3}Z)");
                if (timestampMatch.Success && DateTime.TryParse(timestampMatch.Groups[1].Value, out var timestamp))
                {
                    return timestamp;
                }

                // Fallback: try to find any date pattern
                var dateMatch = Regex.Match(line, @"\d{4}-\d{2}-\d{2}");
                if (dateMatch.Success && DateTime.TryParse(dateMatch.Value, out var date))
                {
                    return date;
                }
            }
            catch { }

            return DateTime.UtcNow;
        }

        /// <summary>
        /// Extracts exception type from log message
        /// </summary>
        private string ExtractExceptionType(string message)
        {
            // Try to find exception type patterns
            var exceptionMatch = Regex.Match(message, @"(\w+Exception|\w+Error)");
            if (exceptionMatch.Success)
            {
                return exceptionMatch.Groups[1].Value;
            }

            if (message.Contains("timeout", StringComparison.OrdinalIgnoreCase))
                return "TimeoutException";
            if (message.Contains("connection", StringComparison.OrdinalIgnoreCase))
                return "ConnectionException";
            if (message.Contains("authentication", StringComparison.OrdinalIgnoreCase))
                return "AuthenticationException";

            return "MongoException";
        }

        /// <summary>
        /// Gets recent MongoDB log entries (last N lines)
        /// </summary>
        public async Task<List<string>> GetRecentLogLinesAsync(int lineCount = 100, CancellationToken cancellationToken = default)
        {
            var lines = new List<string>();

            var logPaths = new[]
            {
                "/var/log/mongodb/mongod.log",
                @"C:\Program Files\MongoDB\Server\7.0\log\mongod.log",
                @"C:\Program Files\MongoDB\Server\6.0\log\mongod.log",
                _settings.LogPath
            };

            foreach (var logPath in logPaths.Where(p => !string.IsNullOrEmpty(p)))
            {
                if (File.Exists(logPath))
                {
                    try
                    {
                        var allLines = await File.ReadAllLinesAsync(logPath, cancellationToken);
                        lines = allLines.TakeLast(lineCount).ToList();
                        break;
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Failed to read log file: {LogPath}", logPath);
                    }
                }
            }

            return lines;
        }
    }
}
