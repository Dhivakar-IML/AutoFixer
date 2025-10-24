using AutoFixer.Models;
using AutoFixer.Services.Interfaces;
using AutoFixer.Data.Repositories;
using AutoFixer.Configuration;
using Microsoft.Extensions.Options;

namespace AutoFixer.Services;

/// <summary>
/// Main service for detecting and analyzing error patterns
/// </summary>
public class PatternDetectionService : IPatternDetectionService
{
    private readonly IErrorPatternRepository _patternRepository;
    private readonly IErrorClusterRepository _clusterRepository;
    private readonly IErrorEntryRepository _errorEntryRepository;
    private readonly ITrendAnalyzer _trendAnalyzer;
    private readonly IFrequencyAnalyzer _frequencyAnalyzer;
    private readonly MLSettings _mlSettings;
    private readonly ILogger<PatternDetectionService> _logger;

    public PatternDetectionService(
        IErrorPatternRepository patternRepository,
        IErrorClusterRepository clusterRepository,
        IErrorEntryRepository errorEntryRepository,
        ITrendAnalyzer trendAnalyzer,
        IFrequencyAnalyzer frequencyAnalyzer,
        IOptions<MLSettings> mlSettings,
        ILogger<PatternDetectionService> logger)
    {
        _patternRepository = patternRepository;
        _clusterRepository = clusterRepository;
        _errorEntryRepository = errorEntryRepository;
        _trendAnalyzer = trendAnalyzer;
        _frequencyAnalyzer = frequencyAnalyzer;
        _mlSettings = mlSettings.Value;
        _logger = logger;
    }

    public async Task<List<ErrorPattern>> DetectNewPatternsAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Starting new pattern detection");
        
        try
        {
            var newPatterns = new List<ErrorPattern>();
            
            // Get recent clusters (last 24 hours) that don't have patterns yet
            var recentClusters = await _clusterRepository.GetRecentClustersWithoutPatternsAsync(TimeSpan.FromHours(24), cancellationToken);
            
            foreach (var cluster in recentClusters)
            {
                if (cancellationToken.IsCancellationRequested)
                    break;

                // Check if this cluster represents a significant pattern
                var pattern = await AnalyzeClusterForPatternAsync(cluster, cancellationToken);
                if (pattern != null)
                {
                    newPatterns.Add(pattern);
                    _logger.LogDebug("Detected new pattern: {PatternName} from cluster {ClusterId}", 
                        pattern.Name, cluster.Id);
                }
            }

            // Save all new patterns
            foreach (var pattern in newPatterns)
            {
                await _patternRepository.CreateAsync(pattern, cancellationToken);
            }

            _logger.LogInformation("Detected {Count} new patterns", newPatterns.Count);
            return newPatterns;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error detecting new patterns");
            return new List<ErrorPattern>();
        }
    }

    public async Task<List<ErrorPattern>> GetTrendingPatternsAsync(TimeSpan timeWindow, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Analyzing trending patterns over {TimeWindow}", timeWindow);
        
        try
        {
            var allPatterns = await _patternRepository.GetActiveAsync(cancellationToken);
            var trendingPatterns = new List<ErrorPattern>();

            foreach (var pattern in allPatterns)
            {
                if (cancellationToken.IsCancellationRequested)
                    break;

                // Analyze trend direction for this pattern
                var trendDirection = await _trendAnalyzer.CalculateTrendDirectionAsync(pattern.Id!, timeWindow, cancellationToken);
                var changeRate = await _trendAnalyzer.CalculateChangeRateAsync(pattern.Id!, timeWindow, cancellationToken);
                var isAccelerating = await _trendAnalyzer.IsAcceleratingAsync(pattern.Id!, timeWindow, cancellationToken);

                // Consider a pattern trending if it's increasing with significant change rate
                if (trendDirection > 0.1 && (changeRate > 0.2 || isAccelerating))
                {
                    // Update pattern with trend information
                    pattern.TrendDirection = trendDirection > 0.1 ? TrendDirection.Increasing :
                                           trendDirection < -0.1 ? TrendDirection.Decreasing :
                                           TrendDirection.Stable;
                    pattern.ChangeRate = changeRate;
                    pattern.IsAccelerating = isAccelerating;
                    pattern.LastAnalyzed = DateTime.UtcNow;
                    
                    trendingPatterns.Add(pattern);
                    
                    _logger.LogDebug("Pattern {PatternName} is trending: direction={Direction:F3}, rate={Rate:F3}, accelerating={Accelerating}",
                        pattern.Name, trendDirection, changeRate, isAccelerating);
                }
            }

            // Sort by most concerning trends first
            trendingPatterns.Sort((a, b) => {
                var aScore = ((int)a.TrendDirection) * (a.ChangeRate ?? 0) * (a.IsAccelerating == true ? 2 : 1);
                var bScore = ((int)b.TrendDirection) * (b.ChangeRate ?? 0) * (b.IsAccelerating == true ? 2 : 1);
                return bScore.CompareTo(aScore);
            });

            _logger.LogInformation("Found {Count} trending patterns", trendingPatterns.Count);
            return trendingPatterns;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error analyzing trending patterns");
            return new List<ErrorPattern>();
        }
    }

    public async Task<ErrorPattern?> AnalyzePatternAsync(string patternId, CancellationToken cancellationToken = default)
    {
        try
        {
            var pattern = await _patternRepository.GetByIdAsync(patternId, cancellationToken);
            if (pattern == null)
            {
                _logger.LogWarning("Pattern {PatternId} not found", patternId);
                return null;
            }

            // Get frequency analysis
            var occurrenceRate = await _frequencyAnalyzer.CalculateOccurrenceRateAsync(patternId, TimeSpan.FromDays(7), cancellationToken);
            var hourlyDistribution = await _frequencyAnalyzer.GetHourlyDistributionAsync(patternId, cancellationToken);
            var weeklyDistribution = await _frequencyAnalyzer.GetWeeklyDistributionAsync(patternId, cancellationToken);

            // Get trend analysis
            var trendDirection = await _trendAnalyzer.CalculateTrendDirectionAsync(patternId, TimeSpan.FromDays(7), cancellationToken);
            var changeRate = await _trendAnalyzer.CalculateChangeRateAsync(patternId, TimeSpan.FromDays(7), cancellationToken);
            var isAccelerating = await _trendAnalyzer.IsAcceleratingAsync(patternId, TimeSpan.FromDays(7), cancellationToken);
            
            // Forecast next period
            var forecast = await _trendAnalyzer.ForecastNextPeriodAsync(patternId, TimeSpan.FromDays(7), TimeSpan.FromDays(1), cancellationToken);

            // Update pattern with analysis results
            pattern.OccurrenceRate = occurrenceRate;
            pattern.TrendDirection = trendDirection > 0.1 ? TrendDirection.Increasing :
                                   trendDirection < -0.1 ? TrendDirection.Decreasing :
                                   TrendDirection.Stable;
            pattern.ChangeRate = changeRate;
            pattern.IsAccelerating = isAccelerating;
            pattern.LastAnalyzed = DateTime.UtcNow;

            // Determine pattern severity based on multiple factors
            pattern.Severity = DetermineSeverity(occurrenceRate, trendDirection, changeRate, isAccelerating);

            // Update pattern in database
            await _patternRepository.UpdateAsync(pattern.Id!, pattern, cancellationToken);

            _logger.LogDebug("Analyzed pattern {PatternName}: rate={Rate:F2}, trend={Trend:F3}, severity={Severity}",
                pattern.Name, occurrenceRate, trendDirection, pattern.Severity);

            return pattern;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error analyzing pattern {PatternId}", patternId);
            return null;
        }
    }

    public async Task<Dictionary<string, List<ErrorPattern>>> FindCorrelatedPatternsAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Finding correlated patterns");
        
        try
        {
            var correlations = new Dictionary<string, List<ErrorPattern>>();
            var allPatterns = await _patternRepository.GetActiveAsync(cancellationToken);

            // Simple correlation analysis based on temporal overlap
            foreach (var pattern1 in allPatterns)
            {
                if (cancellationToken.IsCancellationRequested)
                    break;

                var correlatedPatterns = new List<ErrorPattern>();

                foreach (var pattern2 in allPatterns)
                {
                    if (pattern1.Id == pattern2.Id)
                        continue;

                    // Check if patterns occur around the same times
                    var correlation = await CalculateTemporalCorrelationAsync(pattern1.Id!, pattern2.Id!, cancellationToken);
                    
                    if (correlation > 0.7) // High correlation threshold
                    {
                        correlatedPatterns.Add(pattern2);
                    }
                }

                if (correlatedPatterns.Any())
                {
                    correlations[pattern1.Id!] = correlatedPatterns;
                    _logger.LogDebug("Pattern {PatternName} has {Count} correlated patterns",
                        pattern1.Name, correlatedPatterns.Count);
                }
            }

            _logger.LogInformation("Found correlations for {Count} patterns", correlations.Count);
            return correlations;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error finding correlated patterns");
            return new Dictionary<string, List<ErrorPattern>>();
        }
    }

    private async Task<ErrorPattern?> AnalyzeClusterForPatternAsync(ErrorCluster cluster, CancellationToken cancellationToken)
    {
        try
        {
            // Check if cluster is significant enough to be a pattern
            if (cluster.Occurrences < _mlSettings.MinClusterSize)
                return null;

            // Get errors in this cluster to analyze
            var errors = await _errorEntryRepository.GetByClusterIdAsync(cluster.Id!, cancellationToken);
            if (!errors.Any())
                return null;

            // Calculate occurrence rate
            var timeSpan = cluster.LastSeen - cluster.FirstSeen;
            var occurrenceRate = timeSpan.TotalHours > 0 ? cluster.Occurrences / timeSpan.TotalHours : 0;

            // Only create pattern if occurrence rate is significant
            if (occurrenceRate < 0.1) // Less than 0.1 errors per hour
                return null;

            var pattern = new ErrorPattern
            {
                Id = Guid.NewGuid().ToString(),
                Name = GeneratePatternName(cluster, errors.ToList()),
                Description = GeneratePatternDescription(cluster, errors.ToList()),
                Signature = cluster.PatternSignature,
                ClusterIds = new List<string> { cluster.Id! },
                FirstOccurrence = cluster.FirstSeen,
                LastOccurrence = cluster.LastSeen,
                OccurrenceCount = cluster.Occurrences,
                OccurrenceRate = occurrenceRate,
                AffectedUsers = errors.Select(e => e.UserId).Where(u => !string.IsNullOrEmpty(u)).Distinct().Count(),
                AffectedServices = errors.Select(e => e.Endpoint).Where(s => !string.IsNullOrEmpty(s)).Cast<string>().Distinct().ToList(),
                Severity = DetermineSeverity(occurrenceRate, 0, 0, false),
                Status = PatternStatus.Active,
                CreatedAt = DateTime.UtcNow,
                LastAnalyzed = DateTime.UtcNow
            };

            return pattern;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error analyzing cluster {ClusterId} for pattern", cluster.Id);
            return null;
        }
    }

    private static string GeneratePatternName(ErrorCluster cluster, List<ErrorEntry> errors)
    {
        // Use the most common endpoint and error type
        var mostCommonEndpoint = errors
            .Where(e => !string.IsNullOrEmpty(e.Endpoint))
            .GroupBy(e => e.Endpoint)
            .OrderByDescending(g => g.Count())
            .FirstOrDefault()?.Key ?? "Unknown";

        var mostCommonExceptionType = errors
            .Where(e => !string.IsNullOrEmpty(e.ExceptionType))
            .GroupBy(e => e.ExceptionType)
            .OrderByDescending(g => g.Count())
            .FirstOrDefault()?.Key ?? "Error";

        return $"{mostCommonEndpoint} {mostCommonExceptionType} Pattern";
    }

    private static string GeneratePatternDescription(ErrorCluster cluster, List<ErrorEntry> errors)
    {
        var errorCount = errors.Count;
        var timeSpan = cluster.LastSeen - cluster.FirstSeen;
        var endpoints = errors.Select(e => e.Endpoint).Where(s => !string.IsNullOrEmpty(s)).Distinct().Count();
        
        return $"Pattern detected with {errorCount} errors over {timeSpan.TotalHours:F1} hours across {endpoints} endpoint(s). " +
               $"Signature: {cluster.PatternSignature[..Math.Min(100, cluster.PatternSignature.Length)]}...";
    }

    private static PatternSeverity DetermineSeverity(double occurrenceRate, double trendDirection, double changeRate, bool isAccelerating)
    {
        // High frequency or rapidly increasing patterns are critical
        if (occurrenceRate > 10 || (trendDirection > 0.5 && isAccelerating))
            return PatternSeverity.Critical;

        // Moderate frequency or increasing patterns are high priority
        if (occurrenceRate > 2 || trendDirection > 0.2)
            return PatternSeverity.High;

        // Low but consistent patterns are medium priority
        if (occurrenceRate > 0.5 || trendDirection > 0)
            return PatternSeverity.Medium;

        return PatternSeverity.Low;
    }

    private async Task<double> CalculateTemporalCorrelationAsync(string pattern1Id, string pattern2Id, CancellationToken cancellationToken)
    {
        try
        {
            // Get time series for both patterns
            var timeWindow = TimeSpan.FromDays(7);
            var series1 = await _frequencyAnalyzer.GetOccurrenceTimeSeriesAsync(pattern1Id, timeWindow, cancellationToken);
            var series2 = await _frequencyAnalyzer.GetOccurrenceTimeSeriesAsync(pattern2Id, timeWindow, cancellationToken);

            if (!series1.Any() || !series2.Any())
                return 0.0;

            // Simple Pearson correlation calculation
            var count = Math.Min(series1.Count, series2.Count);
            if (count < 2) return 0.0;

            var sum1 = series1.Take(count).Sum(dp => dp.Count);
            var sum2 = series2.Take(count).Sum(dp => dp.Count);
            var sum1Sq = series1.Take(count).Sum(dp => dp.Count * dp.Count);
            var sum2Sq = series2.Take(count).Sum(dp => dp.Count * dp.Count);
            var sum12 = series1.Take(count).Zip(series2.Take(count), (dp1, dp2) => dp1.Count * dp2.Count).Sum();

            var num = (count * sum12) - (sum1 * sum2);
            var den = Math.Sqrt(((count * sum1Sq) - (sum1 * sum1)) * ((count * sum2Sq) - (sum2 * sum2)));

            return den != 0 ? num / den : 0.0;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calculating temporal correlation between patterns {Pattern1Id} and {Pattern2Id}", pattern1Id, pattern2Id);
            return 0.0;
        }
    }

    public async Task<List<ErrorPattern>> GetPatternsAsync(
        PatternType? type = null, 
        PatternPriority? priority = null, 
        double? minConfidence = null, 
        int? timeframeHours = null, 
        CancellationToken cancellationToken = default)
    {
        try
        {
            var patterns = await _patternRepository.GetActiveAsync(cancellationToken);
            var filteredPatterns = patterns.AsEnumerable();

            if (type.HasValue)
                filteredPatterns = filteredPatterns.Where(p => p.Type == type.Value);

            if (priority.HasValue)
                filteredPatterns = filteredPatterns.Where(p => p.Priority == priority.Value);

            if (minConfidence.HasValue)
                filteredPatterns = filteredPatterns.Where(p => p.Confidence >= minConfidence.Value);

            if (timeframeHours.HasValue)
            {
                var cutoffTime = DateTime.UtcNow.AddHours(-timeframeHours.Value);
                filteredPatterns = filteredPatterns.Where(p => p.LastOccurrence >= cutoffTime);
            }

            return filteredPatterns.OrderByDescending(p => p.LastOccurrence).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting filtered patterns");
            throw;
        }
    }

    public async Task<ErrorPattern?> GetPatternByIdAsync(string id, CancellationToken cancellationToken = default)
    {
        try
        {
            return await _patternRepository.GetByIdAsync(id, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting pattern {PatternId}", id);
            throw;
        }
    }

    public async Task<PatternDetectionResult> AnalyzeRecentPatternsAsync(int timeframeHours, CancellationToken cancellationToken = default)
    {
        var startTime = DateTime.UtcNow;
        _logger.LogInformation("Analyzing recent patterns for {TimeframeHours} hours", timeframeHours);

        try
        {
            // Get existing patterns for comparison
            var existingPatterns = await _patternRepository.GetActiveAsync(cancellationToken);
            var existingPatternCount = existingPatterns.Count();

            // Detect new patterns
            var newPatterns = await DetectNewPatternsAsync(cancellationToken);
            var newPatternCount = newPatterns.Count;

            // Calculate updated patterns
            var updatedPatterns = newPatterns.Count(p => existingPatterns.Any(ep => ep.Id == p.Id));

            var result = new PatternDetectionResult
            {
                NewPatternsDetected = newPatternCount - updatedPatterns,
                PatternsUpdated = updatedPatterns,
                AnalysisDuration = DateTime.UtcNow - startTime,
                DetectedPatterns = newPatterns
            };

            _logger.LogInformation("Pattern analysis complete: {NewPatterns} new, {UpdatedPatterns} updated in {Duration}ms",
                result.NewPatternsDetected, result.PatternsUpdated, result.AnalysisDuration.TotalMilliseconds);

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during recent pattern analysis");
            throw;
        }
    }

    public async Task<PatternStatistics> GetPatternStatisticsAsync(int timeframeHours, CancellationToken cancellationToken = default)
    {
        try
        {
            var cutoffTime = DateTime.UtcNow.AddHours(-timeframeHours);
            var patterns = await _patternRepository.GetActiveAsync(cancellationToken);
            var recentPatterns = patterns.Where(p => p.LastOccurrence >= cutoffTime).ToList();

            var statistics = new PatternStatistics
            {
                TotalPatterns = recentPatterns.Count,
                PatternsByType = recentPatterns
                    .GroupBy(p => p.Type)
                    .ToDictionary(g => g.Key, g => g.Count()),
                PatternsByPriority = recentPatterns
                    .GroupBy(p => p.Priority)
                    .ToDictionary(g => g.Key, g => g.Count()),
                AverageConfidence = recentPatterns.Any() ? recentPatterns.Average(p => p.Confidence) : 0.0,
                MostFrequentPattern = recentPatterns
                    .OrderByDescending(p => p.OccurrenceCount)
                    .FirstOrDefault(),
                HighestImpactPattern = recentPatterns
                    .OrderByDescending(p => p.ImpactScore)
                    .FirstOrDefault()
            };

            return statistics;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting pattern statistics");
            throw;
        }
    }

    public async Task<ErrorPattern?> UpdatePatternAsync(string id, PatternUpdateRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            var pattern = await _patternRepository.GetByIdAsync(id, cancellationToken);
            if (pattern == null)
            {
                _logger.LogWarning("Pattern {PatternId} not found for update", id);
                return null;
            }

            var hasChanges = false;

            if (request.Priority.HasValue && pattern.Priority != request.Priority.Value)
            {
                pattern.Priority = request.Priority.Value;
                hasChanges = true;
            }

            if (!string.IsNullOrEmpty(request.Description) && pattern.Description != request.Description)
            {
                pattern.Description = request.Description;
                hasChanges = true;
            }

            if (!string.IsNullOrEmpty(request.Name) && pattern.Name != request.Name)
            {
                pattern.Name = request.Name;
                hasChanges = true;
            }

            if (hasChanges)
            {
                await _patternRepository.UpdateAsync(pattern.Id!, pattern, cancellationToken);
                _logger.LogInformation("Updated pattern {PatternId}", id);
            }

            return pattern;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating pattern {PatternId}", id);
            throw;
        }
    }
}