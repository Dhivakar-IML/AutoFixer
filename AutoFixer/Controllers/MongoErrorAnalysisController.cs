using Microsoft.AspNetCore.Mvc;
using AutoFixer.Services.Interfaces;
using AutoFixer.Services;
using AutoFixer.Models;
using AutoFixer.Data.Repositories;
using MongoDB.Driver;

namespace AutoFixer.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class MongoErrorAnalysisController : ControllerBase
    {
        private readonly ILogger<MongoErrorAnalysisController> _logger;
        private readonly ILogIngestionService _logIngestionService;
        private readonly IMongoAuditLogIngestionService _mongoAuditService;
        private readonly IErrorPatternRepository _patternRepository;
        private readonly IMongoDatabase _mongoDatabase;
        private readonly MongoLogReaderService _mongoLogReaderService;

        public MongoErrorAnalysisController(
            ILogger<MongoErrorAnalysisController> logger,
            ILogIngestionService logIngestionService,
            IMongoAuditLogIngestionService mongoAuditService,
            IErrorPatternRepository patternRepository,
            IMongoDatabase mongoDatabase,
            MongoLogReaderService mongoLogReaderService)
        {
            _logger = logger;
            _logIngestionService = logIngestionService;
            _mongoAuditService = mongoAuditService;
            _patternRepository = patternRepository;
            _mongoDatabase = mongoDatabase;
            _mongoLogReaderService = mongoLogReaderService;
        }

        [HttpGet("sources")]
        public IActionResult GetMongoErrorSources()
        {
            return Ok(new
            {
                AvailableSources = new[]
                {
                    new { Name = "MongoDB Atlas Cloud", Type = "Live Database", Status = "Connected", Description = "Real-time error patterns stored in cloud database" },
                    new { Name = "MongoDB Audit Logs", Type = "File System", Status = "Configurable", Description = "Authentication failures, failed operations" },
                    new { Name = "MongoDB Server Logs", Type = "File System", Status = "Configurable", Description = "Connection errors, timeouts, performance issues" },
                    new { Name = "Application Driver Errors", Type = "Runtime", Status = "Active", Description = "MongoDB driver connection and operation failures" },
                    new { Name = "Performance Monitoring", Type = "Database Metrics", Status = "Active", Description = "Slow operations, resource exhaustion" }
                },
                ConfigurationPaths = new
                {
                    AuditLogs = new[] { "/var/log/mongodb/audit.log", "C:\\Program Files\\MongoDB\\Server\\log\\audit.log" },
                    ServerLogs = new[] { "/var/log/mongodb/mongod.log", "C:\\Program Files\\MongoDB\\Server\\log\\mongod.log" },
                    EnvironmentVariables = new[] { "MONGODB_AUDIT_LOG", "MONGODB_LOG_PATH" }
                }
            });
        }

        [HttpPost("ingest-mongo-errors")]
        public async Task<IActionResult> IngestMongoErrors([FromQuery] DateTime? since = null)
        {
            try
            {
                var sinceDate = since ?? DateTime.UtcNow.AddHours(-24);
                
                _logger.LogInformation("Starting MongoDB error ingestion since {Since}", sinceDate);

                // Read MongoDB log files
                var logFileErrors = await _mongoLogReaderService.ReadMongoLogsAsync(sinceDate);
                
                // Ingest from MongoDB audit logs
                var mongoErrors = await _mongoAuditService.IngestErrorsAsync(sinceDate);
                
                // Combine all errors
                var allErrors = logFileErrors.Concat(mongoErrors).ToList();
                
                var results = new
                {
                    Success = true,
                    Message = "MongoDB error ingestion completed",
                    ErrorsFound = allErrors.Count,
                    Sources = new
                    {
                        LogFiles = logFileErrors.Count,
                        MongoAuditLogs = mongoErrors.Count(e => e.Source.Contains("Audit")),
                        ServerLogs = logFileErrors.Count(e => e.Source.Contains("Server")),
                        DriverErrors = mongoErrors.Count(e => e.Source.Contains("Driver")),
                        PerformanceIssues = mongoErrors.Count(e => e.Source.Contains("Performance"))
                    },
                    ProcessingTime = DateTime.UtcNow,
                    TimeRange = new { Since = sinceDate, Until = DateTime.UtcNow },
                    LogFileErrorDetails = logFileErrors.Select(e => new
                    {
                        e.Timestamp,
                        Message = e.Message,
                        e.Severity,
                        e.Source,
                        e.ExceptionType,
                        e.StackTrace
                    }).Take(10) // Show first 10 errors as sample
                };

                return Ok(results);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to ingest MongoDB errors");
                return StatusCode(500, new
                {
                    Success = false,
                    Error = ex.Message,
                    Message = "MongoDB error ingestion failed"
                });
            }
        }

        [HttpGet("analyze-mongo-patterns")]
        public async Task<IActionResult> AnalyzeMongoPatterns()
        {
            try
            {
                _logger.LogInformation("Analyzing MongoDB error patterns");

                // Get all patterns from the database
                var allPatterns = await _patternRepository.GetAllAsync();
                
                // Filter MongoDB-related patterns
                var mongoPatterns = allPatterns.Where(p => 
                    p.Name.ToLower().Contains("mongo") ||
                    p.Description.ToLower().Contains("mongo") ||
                    p.Tags.Any(t => t.ToLower().Contains("mongo")) ||
                    p.AffectedServices.Any(s => s.ToLower().Contains("mongo"))
                ).ToList();

                // Analyze pattern categories
                var analysis = new
                {
                    TotalMongoPatterns = mongoPatterns.Count,
                    Categories = mongoPatterns.GroupBy(p => DetermineMongoCategory(p))
                        .Select(g => new { Category = g.Key, Count = g.Count(), Patterns = g.Select(p => p.Name) })
                        .ToList(),
                    SeverityDistribution = mongoPatterns.GroupBy(p => p.Severity)
                        .Select(g => new { Severity = g.Key.ToString(), Count = g.Count() })
                        .ToList(),
                    TrendAnalysis = mongoPatterns.GroupBy(p => p.TrendDirection)
                        .Select(g => new { Trend = g.Key.ToString(), Count = g.Count() })
                        .ToList(),
                    TopIssues = mongoPatterns.OrderByDescending(p => p.Confidence)
                        .Take(5)
                        .Select(p => new 
                        { 
                            p.Name, 
                            Confidence = Math.Round(p.Confidence * 100, 1),
                            p.Priority,
                            p.OccurrenceCount,
                            p.AffectedUsers
                        })
                        .ToList()
                };

                return Ok(analysis);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to analyze MongoDB patterns");
                return StatusCode(500, new { Error = ex.Message });
            }
        }

        [HttpGet("mongo-connection-health")]
        public async Task<IActionResult> CheckMongoConnectionHealth()
        {
            try
            {
                var healthChecks = new Dictionary<string, object>();

                // Test basic connection
                try
                {
                    await _mongoDatabase.RunCommandAsync<MongoDB.Bson.BsonDocument>(new MongoDB.Bson.BsonDocument("ping", 1));
                    healthChecks["BasicConnection"] = new { Status = "Healthy", Message = "MongoDB ping successful" };
                }
                catch (Exception ex)
                {
                    healthChecks["BasicConnection"] = new { Status = "Unhealthy", Message = ex.Message };
                }

                // Test collections access
                try
                {
                    var collections = await _mongoDatabase.ListCollectionNamesAsync();
                    var collectionList = await collections.ToListAsync();
                    healthChecks["Collections"] = new { Status = "Healthy", Count = collectionList.Count, Names = collectionList };
                }
                catch (Exception ex)
                {
                    healthChecks["Collections"] = new { Status = "Unhealthy", Message = ex.Message };
                }

                // Test error patterns collection specifically
                try
                {
                    var patternCount = await _patternRepository.CountAsync(FilterDefinition<ErrorPattern>.Empty);
                    healthChecks["ErrorPatterns"] = new { Status = "Healthy", Count = patternCount };
                }
                catch (Exception ex)
                {
                    healthChecks["ErrorPatterns"] = new { Status = "Unhealthy", Message = ex.Message };
                }

                // Overall health status
                var overallStatus = healthChecks.Values.All(v => 
                {
                    var status = v.GetType().GetProperty("Status")?.GetValue(v)?.ToString();
                    return status == "Healthy";
                }) ? "Healthy" : "Degraded";

                return Ok(new
                {
                    OverallStatus = overallStatus,
                    Timestamp = DateTime.UtcNow,
                    DatabaseName = _mongoDatabase.DatabaseNamespace.DatabaseName,
                    Checks = healthChecks
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "MongoDB health check failed");
                return StatusCode(500, new
                {
                    OverallStatus = "Unhealthy",
                    Error = ex.Message,
                    Timestamp = DateTime.UtcNow
                });
            }
        }

        [HttpGet("mongo-error-trends")]
        public async Task<IActionResult> GetMongoErrorTrends([FromQuery] int hours = 24)
        {
            try
            {
                var since = DateTime.UtcNow.AddHours(-hours);
                var allPatterns = await _patternRepository.GetAllAsync();
                
                var mongoPatterns = allPatterns.Where(p => 
                    p.CreatedAt >= since &&
                    (p.Name.ToLower().Contains("mongo") ||
                     p.Description.ToLower().Contains("mongo") ||
                     p.Tags.Any(t => t.ToLower().Contains("mongo")))
                ).ToList();

                // Group by hour for trend analysis
                var hourlyTrends = mongoPatterns
                    .GroupBy(p => new { p.CreatedAt.Date, p.CreatedAt.Hour })
                    .Select(g => new
                    {
                        DateTime = new DateTime(g.Key.Date.Year, g.Key.Date.Month, g.Key.Date.Day, g.Key.Hour, 0, 0),
                        ErrorCount = g.Count(),
                        AverageConfidence = g.Average(p => p.Confidence),
                        CriticalCount = g.Count(p => p.Priority == PatternPriority.Critical),
                        Categories = g.GroupBy(p => DetermineMongoCategory(p))
                            .Select(cat => new { Category = cat.Key, Count = cat.Count() })
                            .ToList()
                    })
                    .OrderBy(t => t.DateTime)
                    .ToList();

                return Ok(new
                {
                    TimeRange = new { Since = since, Until = DateTime.UtcNow, Hours = hours },
                    TotalPatterns = mongoPatterns.Count,
                    HourlyTrends = hourlyTrends.Select(h => new {
                        h.DateTime,
                        h.ErrorCount,
                        h.AverageConfidence,
                        h.CriticalCount,
                        h.Categories
                    }).ToList(),
                    Summary = new
                    {
                        PeakErrorHour = hourlyTrends.OrderByDescending(h => h.ErrorCount).FirstOrDefault()?.DateTime,
                        AverageErrorsPerHour = hourlyTrends.Any() ? hourlyTrends.Average(h => h.ErrorCount) : 0,
                        TrendDirection = CalculateTrendDirection(hourlyTrends.Cast<dynamic>().ToList())
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get MongoDB error trends");
                return StatusCode(500, new { Error = ex.Message });
            }
        }

        private string DetermineMongoCategory(ErrorPattern pattern)
        {
            var text = $"{pattern.Name} {pattern.Description}".ToLower();
            
            if (text.Contains("connection") || text.Contains("network")) return "Connection";
            if (text.Contains("timeout") || text.Contains("slow")) return "Performance";
            if (text.Contains("auth") || text.Contains("permission")) return "Security";
            if (text.Contains("storage") || text.Contains("disk") || text.Contains("memory")) return "Resources";
            if (text.Contains("query") || text.Contains("operation")) return "Operations";
            
            return "General";
        }

        private string CalculateTrendDirection(List<dynamic> hourlyTrends)
        {
            if (hourlyTrends.Count < 2) return "Stable";
            
            var firstHalf = hourlyTrends.Take(hourlyTrends.Count / 2).Average(h => (double)h.GetType().GetProperty("ErrorCount").GetValue(h));
            var secondHalf = hourlyTrends.Skip(hourlyTrends.Count / 2).Average(h => (double)h.GetType().GetProperty("ErrorCount").GetValue(h));
            
            if (secondHalf > firstHalf * 1.2) return "Increasing";
            if (secondHalf < firstHalf * 0.8) return "Decreasing";
            return "Stable";
        }
    }
}