using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace AutoFixer.Models;

/// <summary>
/// Represents an alert sent for an error pattern
/// </summary>
public class PatternAlert
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; } = string.Empty;

    [BsonElement("patternId")]
    public string PatternId { get; set; } = string.Empty;

    [BsonElement("severity")]
    public AlertSeverity Severity { get; set; }

    [BsonElement("title")]
    public string Title { get; set; } = string.Empty;

    [BsonElement("summary")]
    public string Summary { get; set; } = string.Empty;

    [BsonElement("dashboardUrl")]
    public string? DashboardUrl { get; set; }

    [BsonElement("channel")]
    public AlertChannel Channel { get; set; }

    [BsonElement("sentAt")]
    public DateTime SentAt { get; set; } = DateTime.UtcNow;

    [BsonElement("acknowledged")]
    public bool Acknowledged { get; set; }

    [BsonElement("acknowledgedBy")]
    public string? AcknowledgedBy { get; set; }

    [BsonElement("acknowledgedAt")]
    public DateTime? AcknowledgedAt { get; set; }

    [BsonElement("recipients")]
    public List<string> Recipients { get; set; } = new();

    [BsonElement("suppressUntil")]
    public DateTime? SuppressUntil { get; set; }

    [BsonElement("alertRule")]
    public string? AlertRule { get; set; } // Rule that triggered the alert

    // Additional properties for alert management
    [BsonElement("patternName")]
    public string PatternName { get; set; } = string.Empty;

    [BsonElement("message")]
    public string Message { get; set; } = string.Empty;

    [BsonElement("source")]
    public string Source { get; set; } = string.Empty;

    [BsonElement("environment")]
    public string Environment { get; set; } = string.Empty;

    [BsonElement("status")]
    public AlertStatus Status { get; set; } = AlertStatus.Active;

    [BsonElement("createdAt")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [BsonElement("lastTriggered")]
    public DateTime LastTriggered { get; set; } = DateTime.UtcNow;

    [BsonElement("triggerCount")]
    public int TriggerCount { get; set; } = 1;

    [BsonElement("affectedUsers")]
    public int AffectedUsers { get; set; }

    [BsonElement("affectedServices")]
    public List<string> AffectedServices { get; set; } = new();

    [BsonElement("occurrenceRate")]
    public double OccurrenceRate { get; set; }

    [BsonElement("escalationLevel")]
    public int EscalationLevel { get; set; }

    [BsonElement("lastEscalated")]
    public DateTime? LastEscalated { get; set; }
}

/// <summary>
/// Status of an alert
/// </summary>
public enum AlertStatus
{
    Active = 0,
    Acknowledged = 1,
    Resolved = 2,
    Suppressed = 3
}

/// <summary>
/// Severity levels for pattern alerts
/// </summary>
public enum AlertSeverity
{
    Info = 0,           // New pattern, low impact
    Warning = 1,        // Trending upward, moderate impact
    Critical = 2,       // High frequency, high user impact
    Emergency = 3       // Service degradation detected
}

/// <summary>
/// Channels for sending alerts
/// </summary>
public enum AlertChannel
{
    Slack = 0,
    Teams = 1,
    Email = 2,
    Webhook = 3,
    Dashboard = 4
}

/// <summary>
/// Represents trend analysis for an error pattern
/// </summary>
public class PatternTrend
{
    [BsonElement("patternId")]
    public string PatternId { get; set; } = string.Empty;

    [BsonElement("direction")]
    public TrendDirection Direction { get; set; }

    [BsonElement("changeRate")]
    public double ChangeRate { get; set; } // Percentage change

    [BsonElement("isAccelerating")]
    public bool IsAccelerating { get; set; }

    [BsonElement("forecast")]
    public PatternForecast? Forecast { get; set; }

    [BsonElement("analysisDate")]
    public DateTime AnalysisDate { get; set; } = DateTime.UtcNow;

    [BsonElement("timeWindow")]
    public TimeSpan TimeWindow { get; set; }

    [BsonElement("dataPoints")]
    public List<TrendDataPoint> DataPoints { get; set; } = new();
}

/// <summary>
/// Represents a forecast for pattern behavior
/// </summary>
public class PatternForecast
{
    [BsonElement("predictedOccurrences")]
    public int PredictedOccurrences { get; set; }

    [BsonElement("confidence")]
    public double Confidence { get; set; }

    [BsonElement("forecastPeriod")]
    public TimeSpan ForecastPeriod { get; set; }

    [BsonElement("modelUsed")]
    public string ModelUsed { get; set; } = string.Empty;
}

/// <summary>
/// Represents a data point in trend analysis
/// </summary>
public class TrendDataPoint
{
    [BsonElement("timestamp")]
    public DateTime Timestamp { get; set; }

    [BsonElement("count")]
    public int Count { get; set; }

    [BsonElement("userCount")]
    public int UserCount { get; set; }
}

/// <summary>
/// Represents a rule for suppressing alerts based on conditions
/// </summary>
public class AlertSuppressionRule
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; } = string.Empty;

    [BsonElement("name")]
    public string Name { get; set; } = string.Empty;

    [BsonElement("description")]
    public string Description { get; set; } = string.Empty;

    [BsonElement("conditions")]
    public List<SuppressionCondition> Conditions { get; set; } = new();

    [BsonElement("isActive")]
    public bool IsActive { get; set; } = true;

    [BsonElement("createdBy")]
    public string CreatedBy { get; set; } = string.Empty;

    [BsonElement("createdAt")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [BsonElement("expiresAt")]
    public DateTime? ExpiresAt { get; set; }

    [BsonElement("suppressionCount")]
    public int SuppressionCount { get; set; }

    [BsonElement("timesTriggered")]
    public int TimesTriggered { get; set; } = 0;
}

/// <summary>
/// Represents a condition for alert suppression
/// </summary>
public class SuppressionCondition
{
    [BsonElement("field")]
    public string Field { get; set; } = string.Empty; // e.g., "PatternName", "Severity", "AffectedUsers"

    [BsonElement("operator")]
    public SuppressionOperator Operator { get; set; }

    [BsonElement("value")]
    public string Value { get; set; } = string.Empty;
}

/// <summary>
/// Operators for suppression conditions
/// </summary>
public enum SuppressionOperator
{
    Equals = 0,
    Contains = 1,
    StartsWith = 2,
    EndsWith = 3,
    GreaterThan = 4,
    LessThan = 5,
    NotEquals = 6,
    NotContains = 7,
    Regex = 8,
    GreaterThanOrEqual = 9,
    LessThanOrEqual = 10
}