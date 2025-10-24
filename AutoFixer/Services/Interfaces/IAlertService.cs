using AutoFixer.Models;

namespace AutoFixer.Services.Interfaces;

/// <summary>
/// Interface for alert management and notification services
/// </summary>
public interface IAlertService
{
    /// <summary>
    /// Creates and processes an alert for an error pattern
    /// </summary>
    Task<PatternAlert> CreateAlertAsync(ErrorPattern pattern, AlertSeverity severity, CancellationToken cancellationToken = default);

    /// <summary>
    /// Sends notifications for an alert through configured channels
    /// </summary>
    Task<bool> SendNotificationsAsync(PatternAlert alert, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if alert should be suppressed based on rules
    /// </summary>
    Task<bool> ShouldSuppressAlertAsync(PatternAlert alert, CancellationToken cancellationToken = default);

    /// <summary>
    /// Acknowledges an alert
    /// </summary>
    Task<bool> AcknowledgeAlertAsync(string alertId, string acknowledgedBy, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets active unacknowledged alerts
    /// </summary>
    Task<List<PatternAlert>> GetActiveAlertsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets alerts with optional filtering
    /// </summary>
    Task<List<PatternAlert>> GetAlertsAsync(AlertStatus? status = null, AlertSeverity? severity = null, int? timeframe = null, bool? acknowledged = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a specific alert by ID
    /// </summary>
    Task<PatternAlert?> GetAlertByIdAsync(string alertId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Resolves an alert
    /// </summary>
    Task<bool> ResolveAlertAsync(string alertId, string resolvedBy, string? resolutionNotes = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets alert statistics
    /// </summary>
    Task<AlertStatistics> GetAlertStatisticsAsync(int timeframeHours, CancellationToken cancellationToken = default);

    /// <summary>
    /// Escalates alerts that have been unacknowledged for too long
    /// </summary>
    Task<int> EscalateOverdueAlertsAsync(CancellationToken cancellationToken = default);
}

/// <summary>
/// Interface for Slack notifications
/// </summary>
public interface ISlackNotificationService
{
    Task<bool> SendAlertAsync(PatternAlert alert, string webhookUrl, CancellationToken cancellationToken = default);
    Task<bool> SendEscalationAsync(PatternAlert alert, string webhookUrl, CancellationToken cancellationToken = default);
}

/// <summary>
/// Interface for Teams notifications
/// </summary>
public interface ITeamsNotificationService
{
    Task<bool> SendAlertAsync(PatternAlert alert, string webhookUrl, CancellationToken cancellationToken = default);
    Task<bool> SendEscalationAsync(PatternAlert alert, string webhookUrl, CancellationToken cancellationToken = default);
}

/// <summary>
/// Interface for Email notifications
/// </summary>
public interface IEmailNotificationService
{
    Task<bool> SendAlertAsync(PatternAlert alert, List<string> recipients, CancellationToken cancellationToken = default);
    Task<bool> SendDigestAsync(List<PatternAlert> alerts, List<string> recipients, CancellationToken cancellationToken = default);
}

/// <summary>
/// Interface for alert suppression rules
/// </summary>
public interface IAlertSuppressionService
{
    Task<bool> ShouldSuppressAlertAsync(PatternAlert alert, CancellationToken cancellationToken = default);
    Task<AlertSuppressionRule> CreateSuppressionRuleAsync(string name, string description, List<SuppressionCondition> conditions, TimeSpan? duration = null, bool isActive = true, CancellationToken cancellationToken = default);
    Task<AlertSuppressionRule?> UpdateSuppressionRuleAsync(string ruleId, string? name = null, string? description = null, List<SuppressionCondition>? conditions = null, bool? isActive = null, TimeSpan? duration = null, CancellationToken cancellationToken = default);
    Task<bool> DeleteSuppressionRuleAsync(string ruleId, CancellationToken cancellationToken = default);
    Task<List<AlertSuppressionRule>> GetSuppressionRulesAsync(bool? isActive = null, CancellationToken cancellationToken = default);
    Task<AlertSuppressionRule?> GetSuppressionRuleAsync(string ruleId, CancellationToken cancellationToken = default);
    Task CleanupExpiredRulesAsync(CancellationToken cancellationToken = default);
}