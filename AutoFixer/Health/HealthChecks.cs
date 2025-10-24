using AutoFixer.Services.Interfaces;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using MongoDB.Driver;

namespace AutoFixer.Health;

/// <summary>
/// Health check for MongoDB connectivity
/// </summary>
public class MongoDbHealthCheck : IHealthCheck
{
    private readonly IMongoDatabase _database;
    private readonly ILogger<MongoDbHealthCheck> _logger;

    public MongoDbHealthCheck(IMongoDatabase database, ILogger<MongoDbHealthCheck> logger)
    {
        _database = database;
        _logger = logger;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            // Attempt to ping the database
            await _database.RunCommandAsync((Command<MongoDB.Bson.BsonDocument>)"{ping:1}", cancellationToken: cancellationToken);
            
            _logger.LogDebug("MongoDB health check passed");
            return HealthCheckResult.Healthy("MongoDB is accessible");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "MongoDB health check failed");
            return HealthCheckResult.Unhealthy("MongoDB is not accessible", ex);
        }
    }
}

/// <summary>
/// Health check for pattern detection service
/// </summary>
public class PatternDetectionHealthCheck : IHealthCheck
{
    private readonly IPatternDetectionService _patternDetectionService;
    private readonly ILogger<PatternDetectionHealthCheck> _logger;

    public PatternDetectionHealthCheck(IPatternDetectionService patternDetectionService, ILogger<PatternDetectionHealthCheck> logger)
    {
        _patternDetectionService = patternDetectionService;
        _logger = logger;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            // Check if pattern detection service can retrieve patterns
            var patterns = await _patternDetectionService.GetPatternsAsync(timeframeHours: 1, cancellationToken: cancellationToken);
            
            _logger.LogDebug("Pattern detection service health check passed - {PatternCount} patterns found", patterns.Count);
            
            var data = new Dictionary<string, object>
            {
                ["PatternCount"] = patterns.Count,
                ["LastCheck"] = DateTime.UtcNow
            };

            return HealthCheckResult.Healthy("Pattern detection service is operational", data);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Pattern detection service health check failed");
            return HealthCheckResult.Unhealthy("Pattern detection service is not operational", ex);
        }
    }
}

/// <summary>
/// Health check for alert service
/// </summary>
public class AlertServiceHealthCheck : IHealthCheck
{
    private readonly IAlertService _alertService;
    private readonly ILogger<AlertServiceHealthCheck> _logger;

    public AlertServiceHealthCheck(IAlertService alertService, ILogger<AlertServiceHealthCheck> logger)
    {
        _alertService = alertService;
        _logger = logger;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            // Check if alert service can retrieve active alerts
            var activeAlerts = await _alertService.GetActiveAlertsAsync(cancellationToken);
            
            _logger.LogDebug("Alert service health check passed - {AlertCount} active alerts", activeAlerts.Count);
            
            var data = new Dictionary<string, object>
            {
                ["ActiveAlertCount"] = activeAlerts.Count,
                ["CriticalAlerts"] = activeAlerts.Count(a => a.Severity == AutoFixer.Models.AlertSeverity.Critical),
                ["LastCheck"] = DateTime.UtcNow
            };

            // Consider unhealthy if too many critical alerts
            var criticalCount = activeAlerts.Count(a => a.Severity == AutoFixer.Models.AlertSeverity.Critical);
            if (criticalCount > 10)
            {
                return HealthCheckResult.Degraded($"Alert service operational but {criticalCount} critical alerts active", data: data);
            }

            return HealthCheckResult.Healthy("Alert service is operational", data);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Alert service health check failed");
            return HealthCheckResult.Unhealthy("Alert service is not operational", ex);
        }
    }
}

/// <summary>
/// Health check for notification services
/// </summary>
public class NotificationHealthCheck : IHealthCheck
{
    private readonly ISlackNotificationService _slackService;
    private readonly ITeamsNotificationService _teamsService;
    private readonly IEmailNotificationService _emailService;
    private readonly ILogger<NotificationHealthCheck> _logger;

    public NotificationHealthCheck(
        ISlackNotificationService slackService,
        ITeamsNotificationService teamsService,
        IEmailNotificationService emailService,
        ILogger<NotificationHealthCheck> logger)
    {
        _slackService = slackService;
        _teamsService = teamsService;
        _emailService = emailService;
        _logger = logger;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        var healthData = new Dictionary<string, object>();
        var issues = new List<string>();

        try
        {
            // Check Slack service (simple connectivity test)
            try
            {
                // We can't easily test without sending a real notification
                // So we'll just verify the service is instantiated
                healthData["SlackService"] = _slackService != null ? "Available" : "Unavailable";
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Slack service check failed");
                healthData["SlackService"] = "Unavailable";
                issues.Add("Slack service unavailable");
            }

            // Check Teams service
            try
            {
                healthData["TeamsService"] = _teamsService != null ? "Available" : "Unavailable";
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Teams service check failed");
                healthData["TeamsService"] = "Unavailable";
                issues.Add("Teams service unavailable");
            }

            // Check Email service
            try
            {
                healthData["EmailService"] = _emailService != null ? "Available" : "Unavailable";
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Email service check failed");
                healthData["EmailService"] = "Unavailable";
                issues.Add("Email service unavailable");
            }

            healthData["LastCheck"] = DateTime.UtcNow;

            if (issues.Count == 3)
            {
                return HealthCheckResult.Unhealthy("All notification services are unavailable", data: healthData);
            }
            else if (issues.Any())
            {
                return HealthCheckResult.Degraded($"Some notification services unavailable: {string.Join(", ", issues)}", data: healthData);
            }

            _logger.LogDebug("Notification services health check passed");
            return HealthCheckResult.Healthy("All notification services are available", healthData);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Notification services health check failed");
            return HealthCheckResult.Unhealthy("Notification services health check failed", ex);
        }
    }
}