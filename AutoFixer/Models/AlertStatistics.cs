namespace AutoFixer.Models;

/// <summary>
/// Alert statistics model
/// </summary>
public class AlertStatistics
{
    /// <summary>
    /// Total number of alerts
    /// </summary>
    public int TotalAlerts { get; set; }

    /// <summary>
    /// Active alerts count
    /// </summary>
    public int ActiveAlerts { get; set; }

    /// <summary>
    /// Acknowledged alerts count
    /// </summary>
    public int AcknowledgedAlerts { get; set; }

    /// <summary>
    /// Resolved alerts count
    /// </summary>
    public int ResolvedAlerts { get; set; }

    /// <summary>
    /// Alerts by severity
    /// </summary>
    public Dictionary<AlertSeverity, int> AlertsBySeverity { get; set; } = new();

    /// <summary>
    /// Average acknowledgment time
    /// </summary>
    public TimeSpan? AverageAcknowledgmentTime { get; set; }

    /// <summary>
    /// Average resolution time
    /// </summary>
    public TimeSpan? AverageResolutionTime { get; set; }

    /// <summary>
    /// Alert trend (increasing/decreasing)
    /// </summary>
    public string AlertTrend { get; set; } = "Stable";
}