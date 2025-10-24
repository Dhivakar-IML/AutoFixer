using AutoFixer.Models;
using AutoFixer.Services.Interfaces;
using AutoFixer.Data.Repositories;
using AutoFixer.Configuration;
using Microsoft.Extensions.Options;
using MongoDB.Bson;

namespace AutoFixer.Services;

/// <summary>
/// Main alert service that orchestrates alert creation, suppression, and notifications
/// </summary>
public class AlertService : IAlertService
{
    private readonly IPatternAlertRepository _alertRepository;
    private readonly IAlertSuppressionService _suppressionService;
    private readonly ISlackNotificationService _slackService;
    private readonly ITeamsNotificationService _teamsService;
    private readonly IEmailNotificationService _emailService;
    private readonly SlackSettings _slackSettings;
    private readonly TeamsSettings _teamsSettings;
    private readonly ILogger<AlertService> _logger;

    public AlertService(
        IPatternAlertRepository alertRepository,
        IAlertSuppressionService suppressionService,
        ISlackNotificationService slackService,
        ITeamsNotificationService teamsService,
        IEmailNotificationService emailService,
        IOptions<SlackSettings> slackSettings,
        IOptions<TeamsSettings> teamsSettings,
        ILogger<AlertService> logger)
    {
        _alertRepository = alertRepository;
        _suppressionService = suppressionService;
        _slackService = slackService;
        _teamsService = teamsService;
        _emailService = emailService;
        _slackSettings = slackSettings.Value;
        _teamsSettings = teamsSettings.Value;
        _logger = logger;
    }

    public async Task<PatternAlert> CreateAlertAsync(ErrorPattern pattern, AlertSeverity severity, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Creating alert for pattern {PatternId} with severity {Severity}", pattern.Id, severity);

        try
        {
            // Create alert object
            var alert = new PatternAlert
            {
                Id = ObjectId.GenerateNewId().ToString(),
                PatternId = pattern.Id!,
                PatternName = pattern.Name,
                Severity = severity,
                Title = $"Error Pattern Alert: {pattern.Name}",
                Summary = $"Pattern '{pattern.Name}' has been detected with severity {severity}",
                Message = pattern.Description ?? $"Error pattern detected: {pattern.Name}",
                Source = "AutoFixer",
                Environment = "Production", // TODO: Get from configuration
                Status = AlertStatus.Active,
                CreatedAt = DateTime.UtcNow,
                TriggerCount = pattern.OccurrenceCount,
                AffectedUsers = pattern.AffectedUsers,
                AffectedServices = new List<string>(), // TODO: Get from pattern analysis
                OccurrenceRate = pattern.OccurrenceRate
            };

            // Check if alert should be suppressed
            if (await ShouldSuppressAlertAsync(alert, cancellationToken))
            {
                _logger.LogDebug("Alert suppressed for pattern {PatternId}", pattern.Id);
                return null!; // Alert was suppressed
            }

            // Check for existing active alert for this pattern
            var existingAlerts = await _alertRepository.GetByPatternIdAsync(pattern.Id!, cancellationToken);
            var activeAlert = existingAlerts.FirstOrDefault(a => a.Status == AlertStatus.Active);

            if (activeAlert != null)
            {
                // Update existing alert instead of creating new one
                activeAlert.Severity = severity;
                activeAlert.LastTriggered = DateTime.UtcNow;
                activeAlert.TriggerCount++;
                
                await _alertRepository.UpdateAsync(activeAlert.Id!, activeAlert, cancellationToken);
                _logger.LogDebug("Updated existing alert {AlertId} for pattern {PatternId}", activeAlert.Id, pattern.Id);
                return activeAlert;
            }

            await _alertRepository.CreateAsync(alert, cancellationToken);

            // Send notifications
            await SendNotificationsAsync(alert, cancellationToken);

            _logger.LogInformation("Created and sent alert {AlertId} for pattern {PatternId}", alert.Id, pattern.Id);
            return alert;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating alert for pattern {PatternId}", pattern.Id);
            throw;
        }
    }

    public async Task<bool> SendNotificationsAsync(PatternAlert alert, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Sending notifications for alert {AlertId}", alert.Id);

        var success = true;

        try
        {
            // Send Slack notifications
            if (_slackSettings.Enabled && !string.IsNullOrEmpty(_slackSettings.WebhookUrl))
            {
                var slackSuccess = await _slackService.SendAlertAsync(alert, _slackSettings.WebhookUrl, cancellationToken);
                if (!slackSuccess)
                {
                    _logger.LogWarning("Failed to send Slack notification for alert {AlertId}", alert.Id);
                    success = false;
                }
            }

            // Send Teams notifications
            if (_teamsSettings.Enabled && !string.IsNullOrEmpty(_teamsSettings.WebhookUrl))
            {
                var teamsSuccess = await _teamsService.SendAlertAsync(alert, _teamsSettings.WebhookUrl, cancellationToken);
                if (!teamsSuccess)
                {
                    _logger.LogWarning("Failed to send Teams notification for alert {AlertId}", alert.Id);
                    success = false;
                }
            }

            // Send Email notifications for high severity alerts
            if (alert.Severity >= AlertSeverity.Warning)
            {
                var emailRecipients = GetEmailRecipients(alert.Severity);
                if (emailRecipients.Any())
                {
                    var emailSuccess = await _emailService.SendAlertAsync(alert, emailRecipients, cancellationToken);
                    if (!emailSuccess)
                    {
                        _logger.LogWarning("Failed to send email notification for alert {AlertId}", alert.Id);
                        success = false;
                    }
                }
            }

            return success;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending notifications for alert {AlertId}", alert.Id);
            return false;
        }
    }

    public async Task<bool> ShouldSuppressAlertAsync(PatternAlert alert, CancellationToken cancellationToken = default)
    {
        try
        {
            return await _suppressionService.ShouldSuppressAlertAsync(alert, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking alert suppression for alert {AlertId}", alert.Id);
            return false; // Don't suppress on error
        }
    }

    public async Task<bool> AcknowledgeAlertAsync(string alertId, string acknowledgedBy, CancellationToken cancellationToken = default)
    {
        try
        {
            var result = await _alertRepository.AcknowledgeAlertAsync(alertId, acknowledgedBy, cancellationToken);
            if (result)
            {
                _logger.LogInformation("Alert {AlertId} acknowledged by {User}", alertId, acknowledgedBy);
            }
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error acknowledging alert {AlertId}", alertId);
            return false;
        }
    }

    public async Task<List<PatternAlert>> GetActiveAlertsAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var alerts = await _alertRepository.GetUnacknowledgedAlertsAsync(cancellationToken);
            return alerts.ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting active alerts");
            return new List<PatternAlert>();
        }
    }

    public async Task<int> EscalateOverdueAlertsAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Checking for overdue alerts to escalate");

        try
        {
            var activeAlerts = await GetActiveAlertsAsync(cancellationToken);
            var escalatedCount = 0;

            foreach (var alert in activeAlerts)
            {
                if (cancellationToken.IsCancellationRequested)
                    break;

                var timeOverdue = DateTime.UtcNow - alert.CreatedAt;
                var shouldEscalate = ShouldEscalateAlert(alert, timeOverdue);

                if (shouldEscalate)
                {
                    await EscalateAlertAsync(alert, cancellationToken);
                    escalatedCount++;
                }
            }

            _logger.LogInformation("Escalated {Count} overdue alerts", escalatedCount);
            return escalatedCount;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error escalating overdue alerts");
            return 0;
        }
    }

    private string GenerateAlertMessage(ErrorPattern pattern, AlertSeverity severity)
    {
        var message = severity switch
        {
            AlertSeverity.Critical => $"🚨 CRITICAL: Error pattern '{pattern.Name}' requires immediate attention",
            AlertSeverity.Emergency => $"🚨 EMERGENCY: Error pattern '{pattern.Name}' causing service degradation",
            AlertSeverity.Warning => $"⚠️ WARNING: Error pattern '{pattern.Name}' detected",
            AlertSeverity.Info => $"ℹ️ INFO: Error pattern '{pattern.Name}' observed",
            _ => $"Error pattern '{pattern.Name}' detected"
        };

        if (pattern.AffectedUsers > 0)
        {
            message += $" (Affecting {pattern.AffectedUsers} users)";
        }

        if (pattern.OccurrenceRate > 0)
        {
            message += $" - Rate: {pattern.OccurrenceRate:F1} errors/hour";
        }

        return message;
    }

    private List<string> GetEmailRecipients(AlertSeverity severity)
    {
        // In a real implementation, this would come from configuration or database
        return severity switch
        {
            AlertSeverity.Critical => new List<string> { "on-call@company.com", "engineering-lead@company.com" },
            AlertSeverity.Emergency => new List<string> { "on-call@company.com", "engineering-lead@company.com", "cto@company.com" },
            AlertSeverity.Warning => new List<string> { "dev-team@company.com" },
            _ => new List<string>()
        };
    }

    private bool ShouldEscalateAlert(PatternAlert alert, TimeSpan timeOverdue)
    {
        return alert.Severity switch
        {
            AlertSeverity.Critical => timeOverdue.TotalMinutes > 15, // Escalate critical alerts after 15 minutes
            AlertSeverity.Emergency => timeOverdue.TotalMinutes > 5,  // Escalate emergency alerts after 5 minutes
            AlertSeverity.Warning => timeOverdue.TotalMinutes > 60,   // Escalate warning alerts after 1 hour
            _ => false // Don't escalate info alerts
        };
    }

    private async Task EscalateAlertAsync(PatternAlert alert, CancellationToken cancellationToken)
    {
        try
        {
            alert.EscalationLevel++;
            alert.LastEscalated = DateTime.UtcNow;

            // Send escalation notifications
            if (_slackSettings.Enabled && !string.IsNullOrEmpty(_slackSettings.WebhookUrl))
            {
                await _slackService.SendEscalationAsync(alert, _slackSettings.WebhookUrl, cancellationToken);
            }

            if (_teamsSettings.Enabled && !string.IsNullOrEmpty(_teamsSettings.WebhookUrl))
            {
                await _teamsService.SendEscalationAsync(alert, _teamsSettings.WebhookUrl, cancellationToken);
            }

            // Always send email for escalations
            var escalationRecipients = GetEscalationEmailRecipients(alert.Severity, alert.EscalationLevel);
            if (escalationRecipients.Any())
            {
                await _emailService.SendAlertAsync(alert, escalationRecipients, cancellationToken);
            }

            await _alertRepository.UpdateAsync(alert.Id!, alert, cancellationToken);

            _logger.LogWarning("Escalated alert {AlertId} to level {EscalationLevel}", alert.Id, alert.EscalationLevel);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error escalating alert {AlertId}", alert.Id);
        }
    }

    public async Task<List<PatternAlert>> GetAlertsAsync(
        AlertStatus? status = null, 
        AlertSeverity? severity = null, 
        int? timeframe = null, 
        bool? acknowledged = null, 
        CancellationToken cancellationToken = default)
    {
        try
        {
            return await _alertRepository.GetAlertsAsync(status, severity, timeframe, acknowledged, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting alerts with filters");
            throw;
        }
    }

    public async Task<PatternAlert?> GetAlertByIdAsync(string alertId, CancellationToken cancellationToken = default)
    {
        try
        {
            return await _alertRepository.GetByIdAsync(alertId, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting alert {AlertId}", alertId);
            throw;
        }
    }

    public async Task<bool> ResolveAlertAsync(string alertId, string resolvedBy, string? resolutionNotes = null, CancellationToken cancellationToken = default)
    {
        try
        {
            var alert = await _alertRepository.GetByIdAsync(alertId, cancellationToken);
            if (alert == null)
            {
                _logger.LogWarning("Alert {AlertId} not found for resolution", alertId);
                return false;
            }

            var updates = new Dictionary<string, object>
            {
                ["Status"] = AlertStatus.Resolved,
                ["ResolvedBy"] = resolvedBy,
                ["ResolvedAt"] = DateTime.UtcNow
            };

            if (!string.IsNullOrEmpty(resolutionNotes))
            {
                updates["ResolutionNotes"] = resolutionNotes;
            }

            await _alertRepository.UpdateAsync(alertId, updates, cancellationToken);
            
            _logger.LogInformation("Alert {AlertId} resolved by {ResolvedBy}", alertId, resolvedBy);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error resolving alert {AlertId}", alertId);
            throw;
        }
    }

    public async Task<AlertStatistics> GetAlertStatisticsAsync(int timeframeHours, CancellationToken cancellationToken = default)
    {
        try
        {
            var cutoffTime = DateTime.UtcNow.AddHours(-timeframeHours);
            var alerts = await _alertRepository.GetAlertsFromTimeAsync(cutoffTime, cancellationToken);

            var statistics = new AlertStatistics
            {
                TotalAlerts = alerts.Count,
                ActiveAlerts = alerts.Count(a => a.Status == AlertStatus.Active),
                AcknowledgedAlerts = alerts.Count(a => a.Status == AlertStatus.Acknowledged),
                ResolvedAlerts = alerts.Count(a => a.Status == AlertStatus.Resolved),
                AlertsBySeverity = alerts
                    .GroupBy(a => a.Severity)
                    .ToDictionary(g => g.Key, g => g.Count())
            };

            // Calculate average times
            var acknowledgedAlerts = alerts.Where(a => a.AcknowledgedAt.HasValue).ToList();
            if (acknowledgedAlerts.Any())
            {
                statistics.AverageAcknowledgmentTime = TimeSpan.FromTicks(
                    (long)acknowledgedAlerts.Average(a => (a.AcknowledgedAt!.Value - a.CreatedAt).Ticks));
            }

            // TODO: Add resolution time calculation when ResolvedAt property is added to PatternAlert model

            return statistics;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting alert statistics");
            throw;
        }
    }

    private List<string> GetEscalationEmailRecipients(AlertSeverity severity, int escalationLevel)
    {
        // Escalation recipients based on severity and level
        return (severity, escalationLevel) switch
        {
            (AlertSeverity.Critical, 1) => new List<string> { "engineering-manager@company.com", "cto@company.com" },
            (AlertSeverity.Critical, >= 2) => new List<string> { "ceo@company.com", "engineering-manager@company.com" },
            (AlertSeverity.Emergency, 1) => new List<string> { "engineering-manager@company.com", "cto@company.com" },
            (AlertSeverity.Emergency, >= 2) => new List<string> { "ceo@company.com", "engineering-manager@company.com" },
            (AlertSeverity.Warning, 1) => new List<string> { "engineering-lead@company.com" },
            (AlertSeverity.Warning, >= 2) => new List<string> { "engineering-manager@company.com" },
            _ => new List<string>()
        };
    }
}