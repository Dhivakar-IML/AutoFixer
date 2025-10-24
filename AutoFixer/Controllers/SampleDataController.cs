using AutoFixer.Models;
using AutoFixer.Services.Interfaces;
using AutoFixer.Data.Repositories;
using Microsoft.AspNetCore.Mvc;

namespace AutoFixer.Controllers;

/// <summary>
/// Controller for generating sample data for testing purposes
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class SampleDataController : ControllerBase
{
    private readonly IErrorEntryRepository _errorRepository;
    private readonly IErrorClusterRepository _clusterRepository;
    private readonly IErrorPatternRepository _patternRepository;
    private readonly ILogger<SampleDataController> _logger;

    public SampleDataController(
        IErrorEntryRepository errorRepository,
        IErrorClusterRepository clusterRepository,
        IErrorPatternRepository patternRepository,
        ILogger<SampleDataController> logger)
    {
        _errorRepository = errorRepository;
        _clusterRepository = clusterRepository;
        _patternRepository = patternRepository;
        _logger = logger;
    }

    /// <summary>
    /// Generates sample error data for testing
    /// </summary>
    /// <returns>Confirmation message</returns>
    [HttpPost("generate")]
    [ProducesResponseType(typeof(string), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<string>> GenerateSampleData(CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Generating sample data");

            // Create sample error patterns
            var patterns = new List<ErrorPattern>
            {
                new ErrorPattern
                {
                    Name = "Database Connection Timeout",
                    Description = "Frequent database connection timeouts affecting user operations",
                    Type = PatternType.Persistent,
                    Priority = PatternPriority.High,
                    Confidence = 0.92,
                    PotentialRootCause = "Database server overload or network latency",
                    Status = PatternStatus.Active,
                    Severity = PatternSeverity.High,
                    FirstOccurrence = DateTime.UtcNow.AddDays(-3),
                    LastOccurrence = DateTime.UtcNow.AddMinutes(-15),
                    OccurrenceCount = 247,
                    AffectedUsers = 85,
                    AffectedServices = new List<string> { "UserService", "OrderService", "PaymentService" },
                    OccurrenceRate = 12.5,
                    UserImpact = 85,
                    ImpactScore = 8.5,
                    TrendDirection = TrendDirection.Increasing,
                    Tags = new List<string> { "database", "timeout", "performance" }
                },
                new ErrorPattern
                {
                    Name = "Authentication Service Failures",
                    Description = "Users unable to authenticate during peak hours",
                    Type = PatternType.Trending,
                    Priority = PatternPriority.Critical,
                    Confidence = 0.87,
                    PotentialRootCause = "JWT token service overload during peak traffic",
                    Status = PatternStatus.InProgress,
                    Severity = PatternSeverity.Critical,
                    FirstOccurrence = DateTime.UtcNow.AddDays(-1),
                    LastOccurrence = DateTime.UtcNow.AddMinutes(-5),
                    OccurrenceCount = 156,
                    AffectedUsers = 124,
                    AffectedServices = new List<string> { "AuthService", "UserService" },
                    OccurrenceRate = 18.3,
                    UserImpact = 124,
                    ImpactScore = 9.2,
                    TrendDirection = TrendDirection.Increasing,
                    Tags = new List<string> { "authentication", "jwt", "peak-hours" }
                },
                new ErrorPattern
                {
                    Name = "Memory Leak in Image Processing",
                    Description = "Gradual memory consumption increase in image processing service",
                    Type = PatternType.Persistent,
                    Priority = PatternPriority.Medium,
                    Confidence = 0.76,
                    PotentialRootCause = "Memory not being released after image processing operations",
                    Status = PatternStatus.InvestigationPending,
                    Severity = PatternSeverity.Medium,
                    FirstOccurrence = DateTime.UtcNow.AddDays(-7),
                    LastOccurrence = DateTime.UtcNow.AddHours(-2),
                    OccurrenceCount = 89,
                    AffectedUsers = 45,
                    AffectedServices = new List<string> { "ImageService", "MediaService" },
                    OccurrenceRate = 4.2,
                    UserImpact = 45,
                    ImpactScore = 6.1,
                    TrendDirection = TrendDirection.Stable,
                    Tags = new List<string> { "memory-leak", "image-processing", "performance" }
                },
                new ErrorPattern
                {
                    Name = "API Rate Limiting Triggered",
                    Description = "Third-party API rate limits being exceeded",
                    Type = PatternType.Transient,
                    Priority = PatternPriority.Low,
                    Confidence = 0.94,
                    PotentialRootCause = "Increased traffic causing rate limit violations",
                    Status = PatternStatus.Resolved,
                    Severity = PatternSeverity.Low,
                    FirstOccurrence = DateTime.UtcNow.AddHours(-6),
                    LastOccurrence = DateTime.UtcNow.AddHours(-3),
                    OccurrenceCount = 23,
                    AffectedUsers = 12,
                    AffectedServices = new List<string> { "ExternalAPIService" },
                    OccurrenceRate = 1.8,
                    UserImpact = 12,
                    ImpactScore = 3.4,
                    TrendDirection = TrendDirection.Decreasing,
                    Tags = new List<string> { "rate-limiting", "external-api", "resolved" }
                }
            };

            // Save patterns to database
            foreach (var pattern in patterns)
            {
                await _patternRepository.CreateAsync(pattern, cancellationToken);
            }

            _logger.LogInformation("Successfully generated {Count} sample patterns", patterns.Count);
            
            return Ok($"Successfully generated {patterns.Count} sample error patterns for testing. You can now test the /api/Patterns endpoint.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating sample data");
            return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while generating sample data");
        }
    }

    /// <summary>
    /// Clears all sample data
    /// </summary>
    /// <returns>Confirmation message</returns>
    [HttpDelete("clear")]
    [ProducesResponseType(typeof(string), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<string>> ClearSampleData(CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Clearing sample data");

            // Note: In a real implementation, you might want to be more selective
            // This is just for demo purposes
            var patterns = await _patternRepository.GetActiveAsync(cancellationToken);
            foreach (var pattern in patterns)
            {
                await _patternRepository.DeleteAsync(pattern.Id!, cancellationToken);
            }

            _logger.LogInformation("Cleared all sample data");
            return Ok("Sample data cleared successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error clearing sample data");
            return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while clearing sample data");
        }
    }
}