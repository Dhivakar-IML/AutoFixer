using Microsoft.AspNetCore.Mvc;
using AutoFixer.Integrations.NewRelic;
using AutoFixer.Integrations.Database;
using AutoFixer.Models;
using AutoFixer.Services;

namespace AutoFixer.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class IntegrationsController : ControllerBase
    {
        private readonly NewRelicClient _newRelicClient;
        private readonly IDatabaseIntegrationService _databaseService;
        private readonly IErrorPatternRepository _patternRepository;
        private readonly ILogger<IntegrationsController> _logger;

        public IntegrationsController(
            NewRelicClient newRelicClient,
            IDatabaseIntegrationService databaseService,
            IErrorPatternRepository patternRepository,
            ILogger<IntegrationsController> logger)
        {
            _newRelicClient = newRelicClient;
            _databaseService = databaseService;
            _patternRepository = patternRepository;
            _logger = logger;
        }

        /// <summary>
        /// Import error patterns from New Relic
        /// </summary>
        [HttpPost("newrelic/import")]
        public async Task<ActionResult<IntegrationResult>> ImportFromNewRelic([FromBody] NewRelicImportRequest request)
        {
            try
            {
                _logger.LogInformation("Starting New Relic import for timeframe: {Hours} hours", request.TimeframeHours);

                var since = DateTime.UtcNow.AddHours(-request.TimeframeHours);
                var until = DateTime.UtcNow;

                // Get errors from New Relic
                var errors = await _newRelicClient.GetErrorsAsync(since, until, request.Limit);
                _logger.LogInformation("Retrieved {Count} errors from New Relic", errors.Count);

                // Convert to error patterns
                var patterns = await _newRelicClient.ConvertToErrorPatternsAsync(errors);
                _logger.LogInformation("Converted to {Count} error patterns", patterns.Count);

                // Save patterns to database
                var savedCount = 0;
                foreach (var pattern in patterns)
                {
                    await _patternRepository.CreateAsync(pattern);
                    savedCount++;
                }

                // Get incidents for additional context
                var incidents = await _newRelicClient.GetIncidentsAsync(since);
                _logger.LogInformation("Retrieved {Count} incidents from New Relic", incidents.Count);

                var result = new IntegrationResult
                {
                    Success = true,
                    Source = "New Relic",
                    ErrorsImported = errors.Count,
                    PatternsCreated = savedCount,
                    IncidentsFound = incidents.Count,
                    TimeframeHours = request.TimeframeHours,
                    ImportedAt = DateTime.UtcNow,
                    Message = $"Successfully imported {savedCount} patterns from {errors.Count} New Relic errors"
                };

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error importing from New Relic");
                return BadRequest(new IntegrationResult
                {
                    Success = false,
                    Source = "New Relic",
                    Message = $"Import failed: {ex.Message}"
                });
            }
        }

        /// <summary>
        /// Import error patterns from database logs
        /// </summary>
        [HttpPost("database/import")]
        public async Task<ActionResult<IntegrationResult>> ImportFromDatabase([FromBody] DatabaseImportRequest request)
        {
            try
            {
                _logger.LogInformation("Starting database import from {DbType}", request.DatabaseType);

                List<ErrorPattern> patterns;

                if (!string.IsNullOrEmpty(request.CustomQuery))
                {
                    // Use custom query
                    patterns = request.DatabaseType switch
                    {
                        DatabaseType.SqlServer => await _databaseService.ImportFromSqlServerAsync(
                            request.ConnectionString, request.CustomQuery, DateTime.UtcNow.AddHours(-request.TimeframeHours)),
                        DatabaseType.PostgreSql => await _databaseService.ImportFromPostgreSqlAsync(
                            request.ConnectionString, request.CustomQuery, DateTime.UtcNow.AddHours(-request.TimeframeHours)),
                        DatabaseType.MySql => await _databaseService.ImportFromMySqlAsync(
                            request.ConnectionString, request.CustomQuery, DateTime.UtcNow.AddHours(-request.TimeframeHours)),
                        _ => throw new NotSupportedException($"Database type {request.DatabaseType} not supported")
                    };
                }
                else
                {
                    // Use standard application log import
                    patterns = await _databaseService.ImportApplicationLogsAsync(
                        request.ConnectionString, 
                        request.DatabaseType, 
                        DateTime.UtcNow.AddHours(-request.TimeframeHours));
                }

                _logger.LogInformation("Converted to {Count} error patterns", patterns.Count);

                // Save patterns to database
                var savedCount = 0;
                foreach (var pattern in patterns)
                {
                    await _patternRepository.CreateAsync(pattern);
                    savedCount++;
                }

                var result = new IntegrationResult
                {
                    Success = true,
                    Source = $"Database ({request.DatabaseType})",
                    PatternsCreated = savedCount,
                    TimeframeHours = request.TimeframeHours,
                    ImportedAt = DateTime.UtcNow,
                    Message = $"Successfully imported {savedCount} patterns from database logs"
                };

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error importing from database");
                return BadRequest(new IntegrationResult
                {
                    Success = false,
                    Source = $"Database ({request.DatabaseType})",
                    Message = $"Import failed: {ex.Message}"
                });
            }
        }

        /// <summary>
        /// Test New Relic connection
        /// </summary>
        [HttpPost("newrelic/test")]
        public async Task<ActionResult<ConnectionTestResult>> TestNewRelicConnection()
        {
            try
            {
                var since = DateTime.UtcNow.AddHours(-1);
                var until = DateTime.UtcNow;
                
                var errors = await _newRelicClient.GetErrorsAsync(since, until, 1);
                
                return Ok(new ConnectionTestResult
                {
                    Success = true,
                    Source = "New Relic",
                    Message = "Connection successful",
                    TestTimestamp = DateTime.UtcNow
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "New Relic connection test failed");
                return Ok(new ConnectionTestResult
                {
                    Success = false,
                    Source = "New Relic",
                    Message = $"Connection failed: {ex.Message}",
                    TestTimestamp = DateTime.UtcNow
                });
            }
        }

        /// <summary>
        /// Test database connection
        /// </summary>
        [HttpPost("database/test")]
        public async Task<ActionResult<ConnectionTestResult>> TestDatabaseConnection([FromBody] DatabaseTestRequest request)
        {
            try
            {
                var testQuery = request.DatabaseType switch
                {
                    DatabaseType.SqlServer => "SELECT 1",
                    DatabaseType.PostgreSql => "SELECT 1",
                    DatabaseType.MySql => "SELECT 1",
                    _ => throw new NotSupportedException($"Database type {request.DatabaseType} not supported")
                };

                // Test basic connection
                var patterns = await _databaseService.ImportFromSqlServerAsync(request.ConnectionString, 
                    testQuery.Replace("1", "TOP 1 * FROM sys.tables"), DateTime.UtcNow);

                return Ok(new ConnectionTestResult
                {
                    Success = true,
                    Source = $"Database ({request.DatabaseType})",
                    Message = "Connection successful",
                    TestTimestamp = DateTime.UtcNow
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Database connection test failed");
                return Ok(new ConnectionTestResult
                {
                    Success = false,
                    Source = $"Database ({request.DatabaseType})",
                    Message = $"Connection failed: {ex.Message}",
                    TestTimestamp = DateTime.UtcNow
                });
            }
        }

        /// <summary>
        /// Get integration history
        /// </summary>
        [HttpGet("history")]
        public async Task<ActionResult<List<IntegrationResult>>> GetIntegrationHistory()
        {
            // This would typically come from a database table tracking integrations
            // For now, return mock data showing what successful integrations look like
            var history = new List<IntegrationResult>
            {
                new IntegrationResult
                {
                    Success = true,
                    Source = "New Relic",
                    ErrorsImported = 247,
                    PatternsCreated = 18,
                    IncidentsFound = 3,
                    TimeframeHours = 24,
                    ImportedAt = DateTime.UtcNow.AddHours(-2),
                    Message = "Successfully imported 18 patterns from 247 New Relic errors"
                },
                new IntegrationResult
                {
                    Success = true,
                    Source = "Database (SqlServer)",
                    ErrorsImported = 156,
                    PatternsCreated = 12,
                    TimeframeHours = 48,
                    ImportedAt = DateTime.UtcNow.AddHours(-6),
                    Message = "Successfully imported 12 patterns from database logs"
                }
            };

            return Ok(history);
        }
    }

    // Request/Response Models
    public class NewRelicImportRequest
    {
        public int TimeframeHours { get; set; } = 24;
        public int Limit { get; set; } = 100;
    }

    public class DatabaseImportRequest
    {
        public DatabaseType DatabaseType { get; set; }
        public string ConnectionString { get; set; } = string.Empty;
        public int TimeframeHours { get; set; } = 24;
        public string? CustomQuery { get; set; }
    }

    public class DatabaseTestRequest
    {
        public DatabaseType DatabaseType { get; set; }
        public string ConnectionString { get; set; } = string.Empty;
    }

    public class IntegrationResult
    {
        public bool Success { get; set; }
        public string Source { get; set; } = string.Empty;
        public int ErrorsImported { get; set; }
        public int PatternsCreated { get; set; }
        public int IncidentsFound { get; set; }
        public int TimeframeHours { get; set; }
        public DateTime ImportedAt { get; set; }
        public string Message { get; set; } = string.Empty;
    }

    public class ConnectionTestResult
    {
        public bool Success { get; set; }
        public string Source { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public DateTime TestTimestamp { get; set; }
    }
}