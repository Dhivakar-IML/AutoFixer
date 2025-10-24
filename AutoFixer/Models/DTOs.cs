namespace AutoFixer.Models;

/// <summary>
/// Features extracted from an error for ML clustering
/// </summary>
public class ErrorFeatures
{
    public string ErrorText { get; set; } = string.Empty;
    public string ExceptionType { get; set; } = string.Empty;
    public string Source { get; set; } = string.Empty;
    public string? Endpoint { get; set; }
    public int StatusCode { get; set; }
    public DateTime Timestamp { get; set; }
}

/// <summary>
/// Prediction result from ML model
/// </summary>
public class ErrorPrediction
{
    public float[] Features { get; set; } = Array.Empty<float>();
    public string PredictedCluster { get; set; } = string.Empty;
    public float Confidence { get; set; }
}

/// <summary>
/// Features for cluster analysis
/// </summary>
public class ClusterFeatures
{
    public double OccurrencesPerHour { get; set; }
    public int UniqueUsers { get; set; }
    public int UniqueEndpoints { get; set; }
    public int TimeOfDay { get; set; }
    public int DayOfWeek { get; set; }
    public double ErrorRate { get; set; }
    public string DominantExceptionType { get; set; } = string.Empty;
}

/// <summary>
/// Response structure for dashboard insights
/// </summary>
public class DashboardInsights
{
    public OverviewMetrics Overview { get; set; } = new();
    public List<CriticalPatternSummary> CriticalPatterns { get; set; } = new();
    public List<AlertSummary> RecentAlerts { get; set; } = new();
    public double ResolutionRate { get; set; }
    public TrendSummary Trends { get; set; } = new();
}

/// <summary>
/// Overview metrics for the dashboard
/// </summary>
public class OverviewMetrics
{
    public int TotalPatterns { get; set; }
    public int NewPatterns { get; set; }
    public int ResolvedPatterns { get; set; }
    public int TrendingUp { get; set; }
    public int TotalErrors { get; set; }
    public int AffectedUsers { get; set; }
    public TimeSpan AverageMTTR { get; set; }
}

/// <summary>
/// Summary of a critical pattern for dashboard display
/// </summary>
public class CriticalPatternSummary
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public int Occurrences { get; set; }
    public ErrorSeverity Severity { get; set; }
    public int AffectedUsers { get; set; }
    public TrendDirection Trend { get; set; }
    public string SuggestedAction { get; set; } = string.Empty;
    public DateTime LastOccurrence { get; set; }
}

/// <summary>
/// Summary of an alert for dashboard display
/// </summary>
public class AlertSummary
{
    public string Id { get; set; } = string.Empty;
    public string PatternName { get; set; } = string.Empty;
    public AlertSeverity Severity { get; set; }
    public DateTime SentAt { get; set; }
    public bool Acknowledged { get; set; }
    public string? AcknowledgedBy { get; set; }
}

/// <summary>
/// Summary of trends for dashboard display
/// </summary>
public class TrendSummary
{
    public int PatternsIncreasing { get; set; }
    public int PatternsDecreasing { get; set; }
    public int PatternsStable { get; set; }
    public double OverallTrendScore { get; set; } // Positive = getting worse, Negative = getting better
    public List<string> TopIncreasingPatterns { get; set; } = new();
}

/// <summary>
/// Request model for marking a pattern as resolved
/// </summary>
public class ResolutionRequest
{
    public string SolutionApplied { get; set; } = string.Empty;
    public List<string> PullRequests { get; set; } = new();
    public string? Feedback { get; set; }
    public ResolutionType ResolutionType { get; set; }
    public List<string> VerificationSteps { get; set; } = new();
}