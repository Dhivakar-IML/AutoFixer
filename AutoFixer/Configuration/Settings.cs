namespace AutoFixer.Configuration;

/// <summary>
/// Configuration settings for notification services
/// </summary>
public class NotificationSettings
{
    /// <summary>
    /// Slack notification configuration
    /// </summary>
    public SlackNotificationSettings Slack { get; set; } = new();

    /// <summary>
    /// Microsoft Teams notification configuration
    /// </summary>
    public TeamsNotificationSettings Teams { get; set; } = new();

    /// <summary>
    /// Email notification configuration
    /// </summary>
    public EmailSettings Email { get; set; } = new();
}

/// <summary>
/// Slack notification configuration
/// </summary>
public class SlackNotificationSettings
{
    /// <summary>
    /// Slack webhook URL
    /// </summary>
    public string WebhookUrl { get; set; } = string.Empty;

    /// <summary>
    /// Default channel for notifications
    /// </summary>
    public string Channel { get; set; } = "#alerts";

    /// <summary>
    /// Bot username for notifications
    /// </summary>
    public string Username { get; set; } = "AutoFixer";

    /// <summary>
    /// Bot icon emoji
    /// </summary>
    public string IconEmoji { get; set; } = ":robot_face:";

    /// <summary>
    /// Whether Slack notifications are enabled
    /// </summary>
    public bool IsEnabled { get; set; } = true;

    /// <summary>
    /// Timeout for webhook requests (seconds)
    /// </summary>
    public int TimeoutSeconds { get; set; } = 30;
}

/// <summary>
/// Microsoft Teams notification configuration
/// </summary>
public class TeamsNotificationSettings
{
    /// <summary>
    /// Teams webhook URL
    /// </summary>
    public string WebhookUrl { get; set; } = string.Empty;

    /// <summary>
    /// Default theme color for cards
    /// </summary>
    public string ThemeColor { get; set; } = "FF6B35";

    /// <summary>
    /// Whether Teams notifications are enabled
    /// </summary>
    public bool IsEnabled { get; set; } = true;

    /// <summary>
    /// Timeout for webhook requests (seconds)
    /// </summary>
    public int TimeoutSeconds { get; set; } = 30;
}

/// <summary>
/// Email notification configuration
/// </summary>
public class EmailSettings
{
    /// <summary>
    /// SMTP server hostname
    /// </summary>
    public string SmtpServer { get; set; } = string.Empty;

    /// <summary>
    /// SMTP server port
    /// </summary>
    public int Port { get; set; } = 587;

    /// <summary>
    /// SMTP username
    /// </summary>
    public string Username { get; set; } = string.Empty;

    /// <summary>
    /// SMTP password
    /// </summary>
    public string Password { get; set; } = string.Empty;

    /// <summary>
    /// Whether to use SSL/TLS
    /// </summary>
    public bool EnableSsl { get; set; } = true;

    /// <summary>
    /// From email address
    /// </summary>
    public string FromAddress { get; set; } = string.Empty;

    /// <summary>
    /// From display name
    /// </summary>
    public string FromName { get; set; } = "AutoFixer Alert System";

    /// <summary>
    /// Whether email notifications are enabled
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Default recipients for critical alerts
    /// </summary>
    public List<string> CriticalAlertRecipients { get; set; } = new();

    /// <summary>
    /// Default recipients for warning alerts
    /// </summary>
    public List<string> WarningAlertRecipients { get; set; } = new();
}

/// <summary>
/// Pattern detection configuration
/// </summary>
public class PatternDetectionSettings
{
    /// <summary>
    /// Minimum confidence threshold for pattern detection
    /// </summary>
    public double ConfidenceThreshold { get; set; } = 0.7;

    /// <summary>
    /// Minimum cluster size for pattern creation
    /// </summary>
    public int MinClusterSize { get; set; } = 5;

    /// <summary>
    /// Analysis window in hours
    /// </summary>
    public int AnalysisWindowHours { get; set; } = 24;

    /// <summary>
    /// Correlation threshold for pattern relationships
    /// </summary>
    public double CorrelationThreshold { get; set; } = 0.8;

    /// <summary>
    /// Maximum number of patterns to process in one batch
    /// </summary>
    public int BatchSize { get; set; } = 100;

    /// <summary>
    /// Whether automatic pattern detection is enabled
    /// </summary>
    public bool AutoDetectionEnabled { get; set; } = true;

    /// <summary>
    /// Interval between automatic pattern detection runs (minutes)
    /// </summary>
    public int DetectionIntervalMinutes { get; set; } = 60;
}

/// <summary>
/// Alert escalation configuration
/// </summary>
public class AlertEscalationSettings
{
    /// <summary>
    /// Timeout before escalating critical alerts
    /// </summary>
    public TimeSpan CriticalAlertTimeout { get; set; } = TimeSpan.FromMinutes(15);

    /// <summary>
    /// Timeout before escalating emergency alerts
    /// </summary>
    public TimeSpan EmergencyAlertTimeout { get; set; } = TimeSpan.FromMinutes(5);

    /// <summary>
    /// Timeout before escalating warning alerts
    /// </summary>
    public TimeSpan WarningAlertTimeout { get; set; } = TimeSpan.FromHours(2);

    /// <summary>
    /// Timeout before escalating info alerts
    /// </summary>
    public TimeSpan InfoAlertTimeout { get; set; } = TimeSpan.FromHours(8);

    /// <summary>
    /// Maximum escalation level
    /// </summary>
    public int MaxEscalationLevel { get; set; } = 3;

    /// <summary>
    /// Escalation intervals for each level
    /// </summary>
    public List<TimeSpan> EscalationIntervals { get; set; } = new()
    {
        TimeSpan.FromMinutes(15),
        TimeSpan.FromHours(1),
        TimeSpan.FromHours(4)
    };

    /// <summary>
    /// Whether alert escalation is enabled
    /// </summary>
    public bool EscalationEnabled { get; set; } = true;

    /// <summary>
    /// Interval between escalation checks (minutes)
    /// </summary>
    public int EscalationCheckIntervalMinutes { get; set; } = 5;
}