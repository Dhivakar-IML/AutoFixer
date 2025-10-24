using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace AutoFixer.Models;

/// <summary>
/// Represents a resolution record for an error pattern
/// </summary>
public class PatternResolution
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; } = string.Empty;

    [BsonElement("patternId")]
    public string PatternId { get; set; } = string.Empty;

    [BsonElement("resolvedAt")]
    public DateTime ResolvedAt { get; set; } = DateTime.UtcNow;

    [BsonElement("resolvedBy")]
    public string ResolvedBy { get; set; } = string.Empty;

    [BsonElement("solutionApplied")]
    public string SolutionApplied { get; set; } = string.Empty;

    [BsonElement("pullRequests")]
    public List<string> PullRequests { get; set; } = new();

    [BsonElement("deployedToProduction")]
    public bool DeployedToProduction { get; set; }

    [BsonElement("effectiveness")]
    public double Effectiveness { get; set; } // 0.0 to 1.0

    [BsonElement("feedback")]
    public string? Feedback { get; set; }

    [BsonElement("resolutionType")]
    public ResolutionType ResolutionType { get; set; } = ResolutionType.CodeFix;

    [BsonElement("timeToResolve")]
    public TimeSpan TimeToResolve { get; set; } // Time from pattern identification to resolution

    [BsonElement("verificationSteps")]
    public List<string> VerificationSteps { get; set; } = new();

    [BsonElement("postResolutionMetrics")]
    public PostResolutionMetrics? PostResolutionMetrics { get; set; }
}

/// <summary>
/// Types of resolution approaches
/// </summary>
public enum ResolutionType
{
    CodeFix = 0,
    ConfigurationChange = 1,
    InfrastructureUpdate = 2,
    ProcessChange = 3,
    Ignored = 4,
    WorkaroundImplemented = 5
}

/// <summary>
/// Metrics tracked after a pattern is resolved
/// </summary>
public class PostResolutionMetrics
{
    [BsonElement("errorReductionPercentage")]
    public double ErrorReductionPercentage { get; set; }

    [BsonElement("newOccurrencesAfterFix")]
    public int NewOccurrencesAfterFix { get; set; }

    [BsonElement("userImpactReduction")]
    public int UserImpactReduction { get; set; }

    [BsonElement("measurementPeriodDays")]
    public int MeasurementPeriodDays { get; set; } = 30;

    [BsonElement("relatedIssuesCreated")]
    public int RelatedIssuesCreated { get; set; }

    [BsonElement("customerComplaintsReduction")]
    public int CustomerComplaintsReduction { get; set; }
}