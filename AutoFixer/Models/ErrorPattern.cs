using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace AutoFixer.Models;

/// <summary>
/// Represents a detected error pattern that may span multiple clusters
/// </summary>
public class ErrorPattern
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; } = string.Empty;

    [BsonElement("name")]
    public string Name { get; set; } = string.Empty; // Auto-generated or user-defined

    [BsonElement("description")]
    public string Description { get; set; } = string.Empty;

    [BsonElement("clusterIds")]
    public List<string> ClusterIds { get; set; } = new();

    [BsonElement("type")]
    public PatternType Type { get; set; } // Transient, Persistent, Trending

    [BsonElement("confidence")]
    public double Confidence { get; set; } // ML confidence score

    [BsonElement("potentialRootCause")]
    public string? PotentialRootCause { get; set; }

    [BsonElement("relatedPatterns")]
    public List<string> RelatedPatterns { get; set; } = new();

    [BsonElement("identifiedAt")]
    public DateTime IdentifiedAt { get; set; } = DateTime.UtcNow;

    [BsonElement("status")]
    public PatternStatus Status { get; set; } = PatternStatus.Active;

    [BsonElement("assignedTo")]
    public string? AssignedTo { get; set; }

    [BsonElement("resolutionNotes")]
    public string? ResolutionNotes { get; set; }

    [BsonElement("tags")]
    public List<string> Tags { get; set; } = new();

    [BsonElement("priority")]
    public PatternPriority Priority { get; set; } = PatternPriority.Medium;

    [BsonElement("impactScore")]
    public double ImpactScore { get; set; }

    [BsonElement("trendDirection")]
    public TrendDirection TrendDirection { get; set; } = TrendDirection.Stable;

    [BsonElement("occurrenceRate")]
    public double OccurrenceRate { get; set; } // Occurrences per hour

    [BsonElement("userImpact")]
    public int UserImpact { get; set; } // Number of unique users affected

    // Additional pattern analysis properties
    [BsonElement("signature")]
    public string Signature { get; set; } = string.Empty;

    [BsonElement("firstOccurrence")]
    public DateTime FirstOccurrence { get; set; }

    [BsonElement("lastOccurrence")]
    public DateTime LastOccurrence { get; set; }

    [BsonElement("occurrenceCount")]
    public int OccurrenceCount { get; set; }

    [BsonElement("affectedUsers")]
    public int AffectedUsers { get; set; }

    [BsonElement("affectedServices")]
    public List<string> AffectedServices { get; set; } = new();

    [BsonElement("severity")]
    public PatternSeverity Severity { get; set; } = PatternSeverity.Medium;

    [BsonElement("changeRate")]
    public double? ChangeRate { get; set; }

    [BsonElement("isAccelerating")]
    public bool? IsAccelerating { get; set; }

    [BsonElement("createdAt")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [BsonElement("lastAnalyzed")]
    public DateTime? LastAnalyzed { get; set; }
}

/// <summary>
/// Types of error patterns
/// </summary>
public enum PatternType
{
    Transient = 0,      // Short-lived, self-resolving
    Persistent = 1,     // Ongoing, needs attention
    Trending = 2,       // Increasing frequency
    Cyclic = 3,         // Repeats on schedule
    Correlated = 4      // Appears with other patterns
}

/// <summary>
/// Status of an error pattern
/// </summary>
public enum PatternStatus
{
    Active = 0,
    InvestigationPending = 1,
    InProgress = 2,
    Resolved = 3,
    Ignored = 4,
    Archived = 5
}

/// <summary>
/// Priority levels for error patterns
/// </summary>
public enum PatternPriority
{
    Low = 0,
    Medium = 1,
    High = 2,
    Critical = 3
}

/// <summary>
/// Severity levels for error patterns
/// </summary>
public enum PatternSeverity
{
    Low = 0,
    Medium = 1,
    High = 2,
    Critical = 3
}

/// <summary>
/// Trend direction for pattern analysis
/// </summary>
public enum TrendDirection
{
    Decreasing = -1,
    Stable = 0,
    Increasing = 1
}