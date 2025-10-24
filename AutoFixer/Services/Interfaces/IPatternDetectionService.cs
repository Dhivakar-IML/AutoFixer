using AutoFixer.Models;

namespace AutoFixer.Services.Interfaces;

/// <summary>
/// Interface for pattern detection and analysis
/// </summary>
public interface IPatternDetectionService
{
    Task<List<ErrorPattern>> DetectNewPatternsAsync(CancellationToken cancellationToken = default);
    Task<List<ErrorPattern>> GetTrendingPatternsAsync(TimeSpan timeWindow, CancellationToken cancellationToken = default);
    Task<ErrorPattern?> AnalyzePatternAsync(string patternId, CancellationToken cancellationToken = default);
    Task<Dictionary<string, List<ErrorPattern>>> FindCorrelatedPatternsAsync(CancellationToken cancellationToken = default);
    Task<List<ErrorPattern>> GetPatternsAsync(PatternType? type = null, PatternPriority? priority = null, double? minConfidence = null, int? timeframeHours = null, CancellationToken cancellationToken = default);
    Task<ErrorPattern?> GetPatternByIdAsync(string id, CancellationToken cancellationToken = default);
    Task<PatternDetectionResult> AnalyzeRecentPatternsAsync(int timeframeHours, CancellationToken cancellationToken = default);
    Task<PatternStatistics> GetPatternStatisticsAsync(int timeframeHours, CancellationToken cancellationToken = default);
    Task<ErrorPattern?> UpdatePatternAsync(string id, PatternUpdateRequest request, CancellationToken cancellationToken = default);
}

/// <summary>
/// Interface for trend analysis
/// </summary>
public interface ITrendAnalyzer
{
    Task<double> CalculateTrendDirectionAsync(string patternId, TimeSpan timeWindow, CancellationToken cancellationToken = default);
    Task<double> CalculateChangeRateAsync(string patternId, TimeSpan timeWindow, CancellationToken cancellationToken = default);
    Task<bool> IsAcceleratingAsync(string patternId, TimeSpan timeWindow, CancellationToken cancellationToken = default);
    Task<double> ForecastNextPeriodAsync(string patternId, TimeSpan analysisWindow, TimeSpan forecastPeriod, CancellationToken cancellationToken = default);
}

/// <summary>
/// Interface for frequency analysis
/// </summary>
public interface IFrequencyAnalyzer
{
    Task<List<TrendDataPoint>> GetOccurrenceTimeSeriesAsync(string patternId, TimeSpan timeWindow, CancellationToken cancellationToken = default);
    Task<double> CalculateOccurrenceRateAsync(string patternId, TimeSpan timeWindow, CancellationToken cancellationToken = default);
    Task<Dictionary<string, int>> GetHourlyDistributionAsync(string patternId, CancellationToken cancellationToken = default);
    Task<Dictionary<DayOfWeek, int>> GetWeeklyDistributionAsync(string patternId, CancellationToken cancellationToken = default);
}