namespace AutoFixer.Models;

/// <summary>
/// Result model for pattern detection
/// </summary>
public class PatternDetectionResult
{
    /// <summary>
    /// Number of new patterns detected
    /// </summary>
    public int NewPatternsDetected { get; set; }

    /// <summary>
    /// Number of existing patterns updated
    /// </summary>
    public int PatternsUpdated { get; set; }

    /// <summary>
    /// Analysis duration
    /// </summary>
    public TimeSpan AnalysisDuration { get; set; }

    /// <summary>
    /// List of detected patterns
    /// </summary>
    public List<ErrorPattern> DetectedPatterns { get; set; } = new();
}

/// <summary>
/// Statistics for patterns
/// </summary>
public class PatternStatistics
{
    /// <summary>
    /// Total number of patterns
    /// </summary>
    public int TotalPatterns { get; set; }

    /// <summary>
    /// Patterns by type
    /// </summary>
    public Dictionary<PatternType, int> PatternsByType { get; set; } = new();

    /// <summary>
    /// Patterns by priority
    /// </summary>
    public Dictionary<PatternPriority, int> PatternsByPriority { get; set; } = new();

    /// <summary>
    /// Average confidence score
    /// </summary>
    public double AverageConfidence { get; set; }

    /// <summary>
    /// Most frequent pattern
    /// </summary>
    public ErrorPattern? MostFrequentPattern { get; set; }

    /// <summary>
    /// Highest impact pattern
    /// </summary>
    public ErrorPattern? HighestImpactPattern { get; set; }
}

/// <summary>
/// Request model for pattern updates
/// </summary>
public class PatternUpdateRequest
{
    /// <summary>
    /// New priority level
    /// </summary>
    public PatternPriority? Priority { get; set; }

    /// <summary>
    /// Updated description
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// User-defined name
    /// </summary>
    public string? Name { get; set; }

    /// <summary>
    /// Whether pattern should be monitored
    /// </summary>
    public bool? IsMonitored { get; set; }
}