using Microsoft.AspNetCore.Mvc;
using AutoFixer.Services.Interfaces;

namespace AutoFixer.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class LogIngestionController : ControllerBase
    {
        private readonly ILogger<LogIngestionController> _logger;
        private readonly ILogIngestionService _logIngestionService;
        private readonly INewRelicLogIngestionService _newRelicService;
        private readonly ISeqLogIngestionService _seqService;
        private readonly IMongoAuditLogIngestionService _mongoAuditService;

        public LogIngestionController(
            ILogger<LogIngestionController> logger,
            ILogIngestionService logIngestionService,
            INewRelicLogIngestionService newRelicService,
            ISeqLogIngestionService seqService,
            IMongoAuditLogIngestionService mongoAuditService)
        {
            _logger = logger;
            _logIngestionService = logIngestionService;
            _newRelicService = newRelicService;
            _seqService = seqService;
            _mongoAuditService = mongoAuditService;
        }

        [HttpGet("test-connections")]
        public async Task<IActionResult> TestConnections()
        {
            try
            {
                _logger.LogInformation("Testing all log ingestion service connections");

                var results = new Dictionary<string, object>();

                // Test New Relic connection
                try
                {
                    var newRelicResult = await _newRelicService.TestConnectionAsync();
                    results["NewRelic"] = newRelicResult;
                    _logger.LogInformation("New Relic connection test: {Result}", newRelicResult);
                }
                catch (Exception ex)
                {
                    results["NewRelic"] = false;
                    results["NewRelicError"] = ex.Message;
                    _logger.LogError(ex, "New Relic connection test failed");
                }

                // Test Seq connection
                try
                {
                    var seqResult = await _seqService.TestConnectionAsync();
                    results["Seq"] = seqResult;
                    _logger.LogInformation("Seq connection test: {Result}", seqResult);
                }
                catch (Exception ex)
                {
                    results["Seq"] = false;
                    results["SeqError"] = ex.Message;
                    _logger.LogError(ex, "Seq connection test failed");
                }

                // Test MongoDB Audit connection
                try
                {
                    var mongoResult = await _mongoAuditService.TestConnectionAsync();
                    results["MongoDb"] = mongoResult;
                    _logger.LogInformation("MongoDB Audit connection test: {Result}", mongoResult);
                }
                catch (Exception ex)
                {
                    results["MongoDb"] = false;
                    results["MongoDbError"] = ex.Message;
                    _logger.LogError(ex, "MongoDB Audit connection test failed");
                }

                return Ok(results);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error testing connections");
                return StatusCode(500, new { Error = "Failed to test connections", Details = ex.Message });
            }
        }

        [HttpGet("test-newrelic")]
        public async Task<IActionResult> TestNewRelicConnection()
        {
            try
            {
                _logger.LogInformation("Testing New Relic API connection");
                
                var result = await _newRelicService.TestConnectionAsync();
                
                return Ok(new
                {
                    Success = result,
                    Message = result ? "New Relic API connection successful" : "New Relic API connection failed",
                    Timestamp = DateTime.UtcNow,
                    AccountId = "1050695",
                    ApiKeyStatus = result ? "Valid" : "Invalid or expired"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "New Relic connection test failed");
                return Ok(new
                {
                    Success = false,
                    Message = "New Relic API connection failed",
                    Error = ex.Message,
                    Timestamp = DateTime.UtcNow,
                    AccountId = "1050695",
                    ApiKeyStatus = "Failed to validate"
                });
            }
        }

        [HttpPost("ingest-newrelic")]
        public async Task<IActionResult> IngestFromNewRelic([FromQuery] DateTime? since = null)
        {
            try
            {
                var sinceDate = since ?? DateTime.UtcNow.AddHours(-1);
                
                _logger.LogInformation("Ingesting logs from New Relic since {Since}", sinceDate);
                
                var errors = await _newRelicService.IngestErrorsAsync(sinceDate);
                
                return Ok(new
                {
                    Success = true,
                    Message = $"Successfully ingested {errors.Count()} errors from New Relic",
                    ErrorCount = errors.Count(),
                    Since = sinceDate,
                    Timestamp = DateTime.UtcNow
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to ingest from New Relic");
                return StatusCode(500, new
                {
                    Success = false,
                    Message = "Failed to ingest from New Relic",
                    Error = ex.Message,
                    Timestamp = DateTime.UtcNow
                });
            }
        }
    }
}