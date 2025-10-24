using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace AutoFixer.Models;

/// <summary>
/// Represents a cluster of similar errors grouped by ML algorithms
/// </summary>
public class ErrorCluster
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; } = string.Empty;

    [BsonElement("patternSignature")]
    public string PatternSignature { get; set; } = string.Empty; // Hash of normalized error

    [BsonElement("representativeError")]
    public string RepresentativeError { get; set; } = string.Empty;

    [BsonElement("errorIds")]
    public List<string> ErrorIds { get; set; } = new();

    [BsonElement("occurrences")]
    public int Occurrences { get; set; }

    [BsonElement("firstSeen")]
    public DateTime FirstSeen { get; set; }

    [BsonElement("lastSeen")]
    public DateTime LastSeen { get; set; }

    [BsonElement("severity")]
    public ErrorSeverity Severity { get; set; }

    [BsonElement("suggestedRootCause")]
    public string? SuggestedRootCause { get; set; }

    [BsonElement("affectedUsers")]
    public List<string> AffectedUsers { get; set; } = new();

    [BsonElement("affectedEndpoints")]
    public List<string> AffectedEndpoints { get; set; } = new();

    [BsonElement("status")]
    public ClusterStatus Status { get; set; } = ClusterStatus.Identified;

    [BsonElement("createdAt")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [BsonElement("updatedAt")]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    [BsonElement("mlConfidenceScore")]
    public double MlConfidenceScore { get; set; }

    [BsonElement("patternType")]
    public string? PatternType { get; set; }
}

/// <summary>
/// Status of an error cluster
/// </summary>
public enum ClusterStatus
{
    Identified = 0,
    InProgress = 1,
    Resolved = 2,
    Ignored = 3
}