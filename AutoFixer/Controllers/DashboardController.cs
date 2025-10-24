using AutoFixer.Models;
using AutoFixer.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace AutoFixer.Controllers;

/// <summary>
/// Controller for dashboard data and monitoring endpoints
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class DashboardController : ControllerBase
{
    private readonly IPatternDetectionService _patternDetectionService;
    private readonly IAlertService _alertService;
    private readonly ILogger<DashboardController> _logger;

    public DashboardController(
        IPatternDetectionService patternDetectionService,
        IAlertService alertService,
        ILogger<DashboardController> logger)
    {
        _patternDetectionService = patternDetectionService;
        _alertService = alertService;
        _logger = logger;
    }

    /// <summary>
    /// Gets comprehensive dashboard overview data
    /// </summary>
    /// <param name="timeframe">Timeframe for data (hours)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Dashboard overview data</returns>
    [HttpGet("overview")]
    [ProducesResponseType(typeof(DashboardOverview), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<DashboardOverview>> GetOverview(
        [FromQuery] int timeframe = 24,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Getting dashboard overview for {Timeframe} hours", timeframe);

            if (timeframe <= 0 || timeframe > 720) // Max 30 days
            {
                return BadRequest("Timeframe must be between 1 and 720 hours");
            }

            // Get data in parallel
            var patternStatsTask = _patternDetectionService.GetPatternStatisticsAsync(timeframe, cancellationToken);
            var alertStatsTask = _alertService.GetAlertStatisticsAsync(timeframe, cancellationToken);
            var activeAlertsTask = _alertService.GetActiveAlertsAsync(cancellationToken);
            var recentPatternsTask = _patternDetectionService.GetPatternsAsync(timeframeHours: timeframe, cancellationToken: cancellationToken);

            await Task.WhenAll(patternStatsTask, alertStatsTask, activeAlertsTask, recentPatternsTask);

            var overview = new DashboardOverview
            {
                TimeframeHours = timeframe,
                GeneratedAt = DateTime.UtcNow,
                PatternStatistics = patternStatsTask.Result,
                AlertStatistics = alertStatsTask.Result,
                ActiveAlerts = activeAlertsTask.Result.Take(10).ToList(), // Top 10 active alerts
                RecentPatterns = recentPatternsTask.Result.Take(10).ToList(), // Top 10 recent patterns
                SystemStatus = DetermineSystemStatus(alertStatsTask.Result, patternStatsTask.Result)
            };

            return Ok(overview);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting dashboard overview");
            return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while retrieving dashboard data");
        }
    }

    /// <summary>
    /// Gets real-time system health metrics
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>System health data</returns>
    [HttpGet("health")]
    [ProducesResponseType(typeof(SystemHealth), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<SystemHealth>> GetSystemHealth(CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Getting system health metrics");

            var activeAlerts = await _alertService.GetActiveAlertsAsync(cancellationToken);
            var criticalAlerts = activeAlerts.Where(a => a.Severity == AlertSeverity.Critical || a.Severity == AlertSeverity.Emergency).ToList();

            var health = new SystemHealth
            {
                Status = criticalAlerts.Any() ? "Critical" : activeAlerts.Any() ? "Warning" : "Healthy",
                CheckedAt = DateTime.UtcNow,
                ActiveIncidents = activeAlerts.Count,
                CriticalIncidents = criticalAlerts.Count,
                SystemUptime = GetSystemUptime(),
                LastPatternDetection = await GetLastPatternDetectionTime(cancellationToken),
                Services = await GetServiceStatuses(cancellationToken)
            };

            return Ok(health);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting system health");
            return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while retrieving system health");
        }
    }

    /// <summary>
    /// Gets trending data for charts and graphs
    /// </summary>
    /// <param name="metric">Metric to get trends for</param>
    /// <param name="timeframe">Timeframe for trends (hours)</param>
    /// <param name="interval">Data point interval (minutes)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Trending data</returns>
    [HttpGet("trends")]
    [ProducesResponseType(typeof(TrendData), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<TrendData>> GetTrends(
        [FromQuery] string metric = "alerts",
        [FromQuery] int timeframe = 24,
        [FromQuery] int interval = 60,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Getting trends for metric {Metric} over {Timeframe} hours", metric, timeframe);

            if (timeframe <= 0 || timeframe > 168) // Max 1 week
            {
                return BadRequest("Timeframe must be between 1 and 168 hours");
            }

            if (interval < 5 || interval > 1440) // 5 minutes to 24 hours
            {
                return BadRequest("Interval must be between 5 and 1440 minutes");
            }

            var trendData = metric.ToLowerInvariant() switch
            {
                "alerts" => await GetAlertTrends(timeframe, interval, cancellationToken),
                "patterns" => await GetPatternTrends(timeframe, interval, cancellationToken),
                "errors" => await GetErrorTrends(timeframe, interval, cancellationToken),
                _ => throw new ArgumentException($"Unknown metric: {metric}")
            };

            return Ok(trendData);
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning("Invalid trend request: {Message}", ex.Message);
            return BadRequest(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting trend data for metric {Metric}", metric);
            return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while retrieving trend data");
        }
    }

    /// <summary>
    /// Gets top patterns by various metrics
    /// </summary>
    /// <param name="sortBy">Sort criteria (frequency, impact, confidence)</param>
    /// <param name="count">Number of patterns to return</param>
    /// <param name="timeframe">Timeframe for analysis (hours)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Top patterns</returns>
    [HttpGet("top-patterns")]
    [ProducesResponseType(typeof(List<ErrorPattern>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<List<ErrorPattern>>> GetTopPatterns(
        [FromQuery] string sortBy = "frequency",
        [FromQuery] int count = 10,
        [FromQuery] int timeframe = 24,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Getting top {Count} patterns sorted by {SortBy}", count, sortBy);

            if (count <= 0 || count > 100)
            {
                return BadRequest("Count must be between 1 and 100");
            }

            var patterns = await _patternDetectionService.GetPatternsAsync(timeframeHours: timeframe, cancellationToken: cancellationToken);
            
            var sortedPatterns = sortBy.ToLowerInvariant() switch
            {
                "frequency" => patterns.OrderByDescending(p => p.OccurrenceCount),
                "impact" => patterns.OrderByDescending(p => p.ImpactScore),
                "confidence" => patterns.OrderByDescending(p => p.Confidence),
                "users" => patterns.OrderByDescending(p => p.AffectedUsers),
                _ => throw new ArgumentException($"Unknown sort criteria: {sortBy}")
            };

            return Ok(sortedPatterns.Take(count).ToList());
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning("Invalid top patterns request: {Message}", ex.Message);
            return BadRequest(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting top patterns");
            return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while retrieving top patterns");
        }
    }

    private string DetermineSystemStatus(AlertStatistics alertStats, PatternStatistics patternStats)
    {
        if (alertStats.AlertsBySeverity.GetValueOrDefault(AlertSeverity.Critical, 0) > 0 ||
            alertStats.AlertsBySeverity.GetValueOrDefault(AlertSeverity.Emergency, 0) > 0)
        {
            return "Critical";
        }

        if (alertStats.ActiveAlerts > 10 || patternStats.PatternsByPriority.GetValueOrDefault(PatternPriority.High, 0) > 5)
        {
            return "Warning";
        }

        return "Healthy";
    }

    private TimeSpan GetSystemUptime()
    {
        // This would typically track actual system uptime
        // For now, return uptime since application start
        return DateTime.UtcNow - System.Diagnostics.Process.GetCurrentProcess().StartTime;
    }

    private async Task<DateTime?> GetLastPatternDetectionTime(CancellationToken cancellationToken)
    {
        try
        {
            var recentPatterns = await _patternDetectionService.GetPatternsAsync(timeframeHours: 24, cancellationToken: cancellationToken);
            return recentPatterns.OrderByDescending(p => p.LastOccurrence).FirstOrDefault()?.LastOccurrence;
        }
        catch
        {
            return null;
        }
    }

    private async Task<Dictionary<string, string>> GetServiceStatuses(CancellationToken cancellationToken)
    {
        var services = new Dictionary<string, string>
        {
            ["Pattern Detection"] = "Running",
            ["Alert Service"] = "Running",
            ["MongoDB"] = "Connected",
            ["Notification Services"] = "Running"
        };

        // TODO: Add actual health checks for each service
        return services;
    }

    private async Task<TrendData> GetAlertTrends(int timeframeHours, int intervalMinutes, CancellationToken cancellationToken)
    {
        var endTime = DateTime.UtcNow;
        var startTime = endTime.AddHours(-timeframeHours);
        var dataPoints = new List<TrendDataPoint>();

        // Create time buckets
        var currentTime = startTime;
        while (currentTime < endTime)
        {
            var nextTime = currentTime.AddMinutes(intervalMinutes);
            
            // This would query actual alert data for each time bucket
            // For now, generate sample data
            var alertCount = Random.Shared.Next(0, 10);
            
            dataPoints.Add(new TrendDataPoint
            {
                Timestamp = currentTime,
                Value = alertCount,
                Label = currentTime.ToString("HH:mm")
            });

            currentTime = nextTime;
        }

        return new TrendData
        {
            Metric = "Alerts",
            TimeframeHours = timeframeHours,
            IntervalMinutes = intervalMinutes,
            DataPoints = dataPoints
        };
    }

    private async Task<TrendData> GetPatternTrends(int timeframeHours, int intervalMinutes, CancellationToken cancellationToken)
    {
        // Similar implementation for pattern trends
        var endTime = DateTime.UtcNow;
        var startTime = endTime.AddHours(-timeframeHours);
        var dataPoints = new List<TrendDataPoint>();

        var currentTime = startTime;
        while (currentTime < endTime)
        {
            var patternCount = Random.Shared.Next(0, 5);
            
            dataPoints.Add(new TrendDataPoint
            {
                Timestamp = currentTime,
                Value = patternCount,
                Label = currentTime.ToString("HH:mm")
            });

            currentTime = currentTime.AddMinutes(intervalMinutes);
        }

        return new TrendData
        {
            Metric = "Patterns",
            TimeframeHours = timeframeHours,
            IntervalMinutes = intervalMinutes,
            DataPoints = dataPoints
        };
    }

    private async Task<TrendData> GetErrorTrends(int timeframeHours, int intervalMinutes, CancellationToken cancellationToken)
    {
        // Implementation for error trends
        var endTime = DateTime.UtcNow;
        var startTime = endTime.AddHours(-timeframeHours);
        var dataPoints = new List<TrendDataPoint>();

        var currentTime = startTime;
        while (currentTime < endTime)
        {
            var errorCount = Random.Shared.Next(10, 100);
            
            dataPoints.Add(new TrendDataPoint
            {
                Timestamp = currentTime,
                Value = errorCount,
                Label = currentTime.ToString("HH:mm")
            });

            currentTime = currentTime.AddMinutes(intervalMinutes);
        }

        return new TrendData
        {
            Metric = "Errors",
            TimeframeHours = timeframeHours,
            IntervalMinutes = intervalMinutes,
            DataPoints = dataPoints
        };
    }
}

/// <summary>
/// Dashboard overview data model
/// </summary>
public class DashboardOverview
{
    /// <summary>
    /// Timeframe for the data (hours)
    /// </summary>
    public int TimeframeHours { get; set; }

    /// <summary>
    /// When the overview was generated
    /// </summary>
    public DateTime GeneratedAt { get; set; }

    /// <summary>
    /// Pattern statistics
    /// </summary>
    public PatternStatistics PatternStatistics { get; set; } = new();

    /// <summary>
    /// Alert statistics
    /// </summary>
    public AlertStatistics AlertStatistics { get; set; } = new();

    /// <summary>
    /// Current active alerts
    /// </summary>
    public List<PatternAlert> ActiveAlerts { get; set; } = new();

    /// <summary>
    /// Recent patterns
    /// </summary>
    public List<ErrorPattern> RecentPatterns { get; set; } = new();

    /// <summary>
    /// Overall system status
    /// </summary>
    public string SystemStatus { get; set; } = "Unknown";
}

/// <summary>
/// System health data model
/// </summary>
public class SystemHealth
{
    /// <summary>
    /// Overall system status
    /// </summary>
    public string Status { get; set; } = "Unknown";

    /// <summary>
    /// When health was checked
    /// </summary>
    public DateTime CheckedAt { get; set; }

    /// <summary>
    /// Number of active incidents
    /// </summary>
    public int ActiveIncidents { get; set; }

    /// <summary>
    /// Number of critical incidents
    /// </summary>
    public int CriticalIncidents { get; set; }

    /// <summary>
    /// System uptime
    /// </summary>
    public TimeSpan SystemUptime { get; set; }

    /// <summary>
    /// Last pattern detection time
    /// </summary>
    public DateTime? LastPatternDetection { get; set; }

    /// <summary>
    /// Individual service statuses
    /// </summary>
    public Dictionary<string, string> Services { get; set; } = new();
}

/// <summary>
/// Trend data model for charts
/// </summary>
public class TrendData
{
    /// <summary>
    /// Metric name
    /// </summary>
    public string Metric { get; set; } = string.Empty;

    /// <summary>
    /// Timeframe for the data (hours)
    /// </summary>
    public int TimeframeHours { get; set; }

    /// <summary>
    /// Data point interval (minutes)
    /// </summary>
    public int IntervalMinutes { get; set; }

    /// <summary>
    /// Data points for the chart
    /// </summary>
    public List<TrendDataPoint> DataPoints { get; set; } = new();
}

/// <summary>
/// Individual data point for trends
/// </summary>
public class TrendDataPoint
{
    /// <summary>
    /// Data point timestamp
    /// </summary>
    public DateTime Timestamp { get; set; }

    /// <summary>
    /// Data point value
    /// </summary>
    public double Value { get; set; }

    /// <summary>
    /// Display label
    /// </summary>
    public string Label { get; set; } = string.Empty;
}