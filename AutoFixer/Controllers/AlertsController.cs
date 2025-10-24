using AutoFixer.Models;
using AutoFixer.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace AutoFixer.Controllers;

/// <summary>
/// Controller for managing alerts and alert configuration
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class AlertsController : ControllerBase
{
    private readonly IAlertService _alertService;
    private readonly IAlertSuppressionService _suppressionService;
    private readonly ILogger<AlertsController> _logger;

    public AlertsController(
        IAlertService alertService,
        IAlertSuppressionService suppressionService,
        ILogger<AlertsController> logger)
    {
        _alertService = alertService;
        _suppressionService = suppressionService;
        _logger = logger;
    }

    /// <summary>
    /// Gets all alerts with optional filtering
    /// </summary>
    /// <param name="status">Filter by alert status</param>
    /// <param name="severity">Filter by severity level</param>
    /// <param name="timeframe">Timeframe for alerts (hours)</param>
    /// <param name="acknowledged">Filter by acknowledgment status</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of alerts</returns>
    [HttpGet]
    [ProducesResponseType(typeof(List<PatternAlert>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<List<PatternAlert>>> GetAlerts(
        [FromQuery] AlertStatus? status = null,
        [FromQuery] AlertSeverity? severity = null,
        [FromQuery] int? timeframe = null,
        [FromQuery] bool? acknowledged = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Getting alerts with filters - Status: {Status}, Severity: {Severity}, Timeframe: {Timeframe}, Acknowledged: {Acknowledged}",
                status, severity, timeframe, acknowledged);

            var alerts = await _alertService.GetAlertsAsync(status, severity, timeframe, acknowledged, cancellationToken);

            return Ok(alerts);
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning("Invalid parameters for getting alerts: {Message}", ex.Message);
            return BadRequest(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting alerts");
            return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while retrieving alerts");
        }
    }

    /// <summary>
    /// Gets a specific alert by ID
    /// </summary>
    /// <param name="id">Alert ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Alert details</returns>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(PatternAlert), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<PatternAlert>> GetAlert(string id, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Getting alert {AlertId}", id);

            var alert = await _alertService.GetAlertByIdAsync(id, cancellationToken);
            
            if (alert == null)
            {
                _logger.LogWarning("Alert {AlertId} not found", id);
                return NotFound($"Alert with ID {id} not found");
            }

            return Ok(alert);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting alert {AlertId}", id);
            return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while retrieving the alert");
        }
    }

    /// <summary>
    /// Acknowledges an alert
    /// </summary>
    /// <param name="id">Alert ID</param>
    /// <param name="request">Acknowledgment request</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Success status</returns>
    [HttpPost("{id}/acknowledge")]
    [ProducesResponseType(typeof(AlertAcknowledgmentResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<AlertAcknowledgmentResponse>> AcknowledgeAlert(
        string id,
        [FromBody] AlertAcknowledgmentRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Acknowledging alert {AlertId} by {User}", id, request.AcknowledgedBy);

            if (string.IsNullOrWhiteSpace(request.AcknowledgedBy))
            {
                return BadRequest("AcknowledgedBy is required");
            }

            var success = await _alertService.AcknowledgeAlertAsync(id, request.AcknowledgedBy, cancellationToken);

            if (!success)
            {
                _logger.LogWarning("Failed to acknowledge alert {AlertId} - not found or already acknowledged", id);
                return NotFound($"Alert with ID {id} not found or already acknowledged");
            }

            var response = new AlertAcknowledgmentResponse
            {
                Success = true,
                AcknowledgedBy = request.AcknowledgedBy,
                AcknowledgedAt = DateTime.UtcNow,
                Message = "Alert acknowledged successfully"
            };

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error acknowledging alert {AlertId}", id);
            return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while acknowledging the alert");
        }
    }

    /// <summary>
    /// Resolves an alert
    /// </summary>
    /// <param name="id">Alert ID</param>
    /// <param name="request">Resolution request</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Success status</returns>
    [HttpPost("{id}/resolve")]
    [ProducesResponseType(typeof(AlertResolutionResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<AlertResolutionResponse>> ResolveAlert(
        string id,
        [FromBody] AlertResolutionRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Resolving alert {AlertId} by {User}", id, request.ResolvedBy);

            if (string.IsNullOrWhiteSpace(request.ResolvedBy))
            {
                return BadRequest("ResolvedBy is required");
            }

            var success = await _alertService.ResolveAlertAsync(id, request.ResolvedBy, request.ResolutionNotes, cancellationToken);

            if (!success)
            {
                _logger.LogWarning("Failed to resolve alert {AlertId} - not found", id);
                return NotFound($"Alert with ID {id} not found");
            }

            var response = new AlertResolutionResponse
            {
                Success = true,
                ResolvedBy = request.ResolvedBy,
                ResolvedAt = DateTime.UtcNow,
                ResolutionNotes = request.ResolutionNotes,
                Message = "Alert resolved successfully"
            };

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error resolving alert {AlertId}", id);
            return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while resolving the alert");
        }
    }

    /// <summary>
    /// Gets active unacknowledged alerts for dashboard
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of active alerts</returns>
    [HttpGet("active")]
    [ProducesResponseType(typeof(List<PatternAlert>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<List<PatternAlert>>> GetActiveAlerts(CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Getting active alerts");

            var alerts = await _alertService.GetActiveAlertsAsync(cancellationToken);

            return Ok(alerts);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting active alerts");
            return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while retrieving active alerts");
        }
    }

    /// <summary>
    /// Gets alert statistics and metrics
    /// </summary>
    /// <param name="timeframe">Timeframe for statistics (hours)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Alert statistics</returns>
    [HttpGet("statistics")]
    [ProducesResponseType(typeof(AlertStatistics), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<AlertStatistics>> GetAlertStatistics(
        [FromQuery] int timeframe = 24,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Getting alert statistics for {Timeframe} hours", timeframe);

            if (timeframe <= 0 || timeframe > 720) // Max 30 days
            {
                return BadRequest("Timeframe must be between 1 and 720 hours");
            }

            var statistics = await _alertService.GetAlertStatisticsAsync(timeframe, cancellationToken);

            return Ok(statistics);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting alert statistics");
            return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while retrieving statistics");
        }
    }

    /// <summary>
    /// Triggers escalation check for overdue alerts
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Escalation results</returns>
    [HttpPost("escalate")]
    [ProducesResponseType(typeof(EscalationResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<EscalationResult>> TriggerEscalation(CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Triggering alert escalation check");

            var escalatedCount = await _alertService.EscalateOverdueAlertsAsync(cancellationToken);

            var result = new EscalationResult
            {
                EscalatedAlerts = escalatedCount,
                ProcessedAt = DateTime.UtcNow,
                Success = true
            };

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during alert escalation");
            return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred during escalation");
        }
    }
}

/// <summary>
/// Request model for alert acknowledgment
/// </summary>
public class AlertAcknowledgmentRequest
{
    /// <summary>
    /// User acknowledging the alert
    /// </summary>
    public string AcknowledgedBy { get; set; } = string.Empty;

    /// <summary>
    /// Optional acknowledgment notes
    /// </summary>
    public string? Notes { get; set; }
}

/// <summary>
/// Response model for alert acknowledgment
/// </summary>
public class AlertAcknowledgmentResponse
{
    /// <summary>
    /// Whether acknowledgment was successful
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// User who acknowledged the alert
    /// </summary>
    public string AcknowledgedBy { get; set; } = string.Empty;

    /// <summary>
    /// When the alert was acknowledged
    /// </summary>
    public DateTime AcknowledgedAt { get; set; }

    /// <summary>
    /// Response message
    /// </summary>
    public string Message { get; set; } = string.Empty;
}

/// <summary>
/// Request model for alert resolution
/// </summary>
public class AlertResolutionRequest
{
    /// <summary>
    /// User resolving the alert
    /// </summary>
    public string ResolvedBy { get; set; } = string.Empty;

    /// <summary>
    /// Resolution notes
    /// </summary>
    public string? ResolutionNotes { get; set; }
}

/// <summary>
/// Response model for alert resolution
/// </summary>
public class AlertResolutionResponse
{
    /// <summary>
    /// Whether resolution was successful
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// User who resolved the alert
    /// </summary>
    public string ResolvedBy { get; set; } = string.Empty;

    /// <summary>
    /// When the alert was resolved
    /// </summary>
    public DateTime ResolvedAt { get; set; }

    /// <summary>
    /// Resolution notes
    /// </summary>
    public string? ResolutionNotes { get; set; }

    /// <summary>
    /// Response message
    /// </summary>
    public string Message { get; set; } = string.Empty;
}

/// <summary>
/// Escalation result model
/// </summary>
public class EscalationResult
{
    /// <summary>
    /// Number of alerts escalated
    /// </summary>
    public int EscalatedAlerts { get; set; }

    /// <summary>
    /// When escalation was processed
    /// </summary>
    public DateTime ProcessedAt { get; set; }

    /// <summary>
    /// Whether escalation was successful
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// Any error messages
    /// </summary>
    public string? ErrorMessage { get; set; }
}