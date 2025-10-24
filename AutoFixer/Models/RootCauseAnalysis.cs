using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace AutoFixer.Models;

/// <summary>
/// Represents the result of root cause analysis for an error pattern
/// </summary>
public class RootCauseAnalysis
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; } = string.Empty;

    [BsonElement("patternId")]
    public string PatternId { get; set; } = string.Empty;

    [BsonElement("hypotheses")]
    public List<RootCauseHypothesis> Hypotheses { get; set; } = new();

    [BsonElement("affectedComponents")]
    public List<string> AffectedComponents { get; set; } = new();

    [BsonElement("affectedDependencies")]
    public List<string> AffectedDependencies { get; set; } = new();

    [BsonElement("confidenceScores")]
    public Dictionary<string, double> ConfidenceScores { get; set; } = new();

    [BsonElement("relatedCodeLocations")]
    public List<string> RelatedCodeLocations { get; set; } = new();

    [BsonElement("relatedPullRequests")]
    public List<string> RelatedPullRequests { get; set; } = new();

    [BsonElement("createdAt")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [BsonElement("updatedAt")]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    [BsonElement("analysisVersion")]
    public string AnalysisVersion { get; set; } = "1.0";
}

/// <summary>
/// Represents a hypothesis about the root cause of an error pattern
/// </summary>
public class RootCauseHypothesis
{
    [BsonElement("description")]
    public string Description { get; set; } = string.Empty;

    [BsonElement("confidence")]
    public double Confidence { get; set; }

    [BsonElement("supportingEvidence")]
    public List<string> SupportingEvidence { get; set; } = new();

    [BsonElement("suggestions")]
    public List<SolutionSuggestion> Suggestions { get; set; } = new();

    [BsonElement("category")]
    public string Category { get; set; } = string.Empty; // e.g., "Configuration", "Code Logic", "Infrastructure"

    [BsonElement("severity")]
    public HypothesisSeverity Severity { get; set; } = HypothesisSeverity.Medium;
}

/// <summary>
/// Represents a suggested solution for addressing an error pattern
/// </summary>
public class SolutionSuggestion
{
    [BsonElement("description")]
    public string Description { get; set; } = string.Empty;

    [BsonElement("codeExample")]
    public string? CodeExample { get; set; }

    [BsonElement("relatedDocumentation")]
    public List<string> RelatedDocumentation { get; set; } = new();

    [BsonElement("similarHistoricalFixes")]
    public List<string> SimilarHistoricalFixes { get; set; } = new();

    [BsonElement("timesSuccessful")]
    public int TimesSuccessful { get; set; } // Based on feedback

    [BsonElement("estimatedEffort")]
    public string? EstimatedEffort { get; set; } // e.g., "Low", "Medium", "High"

    [BsonElement("riskLevel")]
    public string? RiskLevel { get; set; } // e.g., "Low", "Medium", "High"

    [BsonElement("category")]
    public SolutionCategory Category { get; set; } = SolutionCategory.CodeFix;
}

/// <summary>
/// Severity levels for root cause hypotheses
/// </summary>
public enum HypothesisSeverity
{
    Low = 0,
    Medium = 1,
    High = 2,
    Critical = 3
}

/// <summary>
/// Categories for solution suggestions
/// </summary>
public enum SolutionCategory
{
    CodeFix = 0,
    Configuration = 1,
    Infrastructure = 2,
    Monitoring = 3,
    Documentation = 4,
    ProcessImprovement = 5
}