using AutoFixer.Models;

namespace AutoFixer.Services.Interfaces;

/// <summary>
/// Interface for root cause analysis engine
/// </summary>
public interface IRootCauseAnalysisEngine
{
    /// <summary>
    /// Analyzes an error pattern and generates potential root causes
    /// </summary>
    Task<RootCauseAnalysis> AnalyzePatternAsync(ErrorPattern pattern, CancellationToken cancellationToken = default);

    /// <summary>
    /// Generates hypotheses for potential root causes based on error data
    /// </summary>
    Task<List<RootCauseHypothesis>> GenerateHypothesesAsync(ErrorPattern pattern, List<ErrorEntry> errors, CancellationToken cancellationToken = default);

    /// <summary>
    /// Suggests solutions based on identified root causes
    /// </summary>
    Task<List<SolutionSuggestion>> GenerateSolutionsAsync(RootCauseAnalysis analysis, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates confidence scores for hypotheses based on new evidence
    /// </summary>
    Task<bool> UpdateConfidenceScoresAsync(string analysisId, List<RootCauseHypothesis> hypotheses, CancellationToken cancellationToken = default);

    /// <summary>
    /// Learns from successful resolutions to improve future analysis
    /// </summary>
    Task<bool> LearnFromResolutionAsync(string patternId, PatternResolution resolution, CancellationToken cancellationToken = default);
}