using AutoFixer.Models;
using AutoFixer.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace AutoFixer.Controllers;

/// <summary>
/// Controller for managing error patterns and pattern detection
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class PatternsController : ControllerBase
{
    private readonly IPatternDetectionService _patternDetectionService;
    private readonly ILogger<PatternsController> _logger;

    public PatternsController(
        IPatternDetectionService patternDetectionService,
        ILogger<PatternsController> logger)
    {
        _patternDetectionService = patternDetectionService;
        _logger = logger;
    }

    /// <summary>
    /// Gets all detected error patterns with optional filtering
    /// </summary>
    /// <param name="type">Filter by pattern type (Transient, Persistent, Trending)</param>
    /// <param name="priority">Filter by priority level</param>
    /// <param name="minConfidence">Minimum confidence score (0.0 to 1.0)</param>
    /// <param name="timeframe">Timeframe for patterns (hours)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of error patterns</returns>
    [HttpGet]
    [ProducesResponseType(typeof(List<ErrorPattern>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<List<ErrorPattern>>> GetPatterns(
        [FromQuery] PatternType? type = null,
        [FromQuery] PatternPriority? priority = null,
        [FromQuery] double? minConfidence = null,
        [FromQuery] int? timeframe = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Getting patterns with filters - Type: {Type}, Priority: {Priority}, MinConfidence: {MinConfidence}, Timeframe: {Timeframe}",
                type, priority, minConfidence, timeframe);

            var patterns = await _patternDetectionService.GetPatternsAsync(
                type, priority, minConfidence, timeframe, cancellationToken);

            return Ok(patterns);
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning("Invalid parameters for getting patterns: {Message}", ex.Message);
            return BadRequest(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting patterns");
            return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while retrieving patterns");
        }
    }

    /// <summary>
    /// Gets a specific error pattern by ID
    /// </summary>
    /// <param name="id">Pattern ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Error pattern details</returns>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(ErrorPattern), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ErrorPattern>> GetPattern(string id, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Getting pattern {PatternId}", id);

            var pattern = await _patternDetectionService.GetPatternByIdAsync(id, cancellationToken);
            
            if (pattern == null)
            {
                _logger.LogWarning("Pattern {PatternId} not found", id);
                return NotFound($"Pattern with ID {id} not found");
            }

            return Ok(pattern);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting pattern {PatternId}", id);
            return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while retrieving the pattern");
        }
    }

    /// <summary>
    /// Triggers pattern detection analysis manually
    /// </summary>
    /// <param name="request">Pattern detection request parameters</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Detection results</returns>
    [HttpPost("detect")]
    [ProducesResponseType(typeof(PatternDetectionResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<PatternDetectionResult>> TriggerPatternDetection(
        [FromBody] PatternDetectionRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Triggering pattern detection for timeframe {TimeframeHours} hours", request.TimeframeHours);

            if (request.TimeframeHours <= 0 || request.TimeframeHours > 168) // Max 1 week
            {
                return BadRequest("Timeframe must be between 1 and 168 hours");
            }

            var result = await _patternDetectionService.AnalyzeRecentPatternsAsync(
                request.TimeframeHours, cancellationToken);

            return Ok(result);
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning("Invalid pattern detection request: {Message}", ex.Message);
            return BadRequest(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during pattern detection");
            return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred during pattern detection");
        }
    }

    /// <summary>
    /// Gets pattern statistics and metrics
    /// </summary>
    /// <param name="timeframe">Timeframe for statistics (hours)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Pattern statistics</returns>
    [HttpGet("statistics")]
    [ProducesResponseType(typeof(PatternStatistics), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<PatternStatistics>> GetPatternStatistics(
        [FromQuery] int timeframe = 24,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Getting pattern statistics for {Timeframe} hours", timeframe);

            if (timeframe <= 0 || timeframe > 720) // Max 30 days
            {
                return BadRequest("Timeframe must be between 1 and 720 hours");
            }

            var statistics = await _patternDetectionService.GetPatternStatisticsAsync(timeframe, cancellationToken);

            return Ok(statistics);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting pattern statistics");
            return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while retrieving statistics");
        }
    }

    /// <summary>
    /// Updates pattern priority or other metadata
    /// </summary>
    /// <param name="id">Pattern ID</param>
    /// <param name="request">Update request</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Updated pattern</returns>
    [HttpPatch("{id}")]
    [ProducesResponseType(typeof(ErrorPattern), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ErrorPattern>> UpdatePattern(
        string id,
        [FromBody] PatternUpdateRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Updating pattern {PatternId}", id);

            var updatedPattern = await _patternDetectionService.UpdatePatternAsync(
                id, request, cancellationToken);

            if (updatedPattern == null)
            {
                _logger.LogWarning("Pattern {PatternId} not found for update", id);
                return NotFound($"Pattern with ID {id} not found");
            }

            return Ok(updatedPattern);
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning("Invalid pattern update request: {Message}", ex.Message);
            return BadRequest(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating pattern {PatternId}", id);
            return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while updating the pattern");
        }
    }
}

/// <summary>
/// Request model for pattern detection
/// </summary>
public class PatternDetectionRequest
{
    /// <summary>
    /// Timeframe for analysis in hours
    /// </summary>
    public int TimeframeHours { get; set; } = 24;

    /// <summary>
    /// Minimum confidence threshold
    /// </summary>
    public double MinConfidence { get; set; } = 0.7;

    /// <summary>
    /// Force re-analysis of existing patterns
    /// </summary>
    public bool ForceReanalysis { get; set; } = false;
}