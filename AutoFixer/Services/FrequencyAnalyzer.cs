using AutoFixer.Models;
using AutoFixer.Services.Interfaces;
using AutoFixer.Data.Repositories;

namespace AutoFixer.Services;

/// <summary>
/// Service for analyzing frequency patterns in errors
/// </summary>
public class FrequencyAnalyzer : IFrequencyAnalyzer
{
    private readonly IErrorEntryRepository _errorEntryRepository;
    private readonly IErrorClusterRepository _clusterRepository;
    private readonly ILogger<FrequencyAnalyzer> _logger;

    public FrequencyAnalyzer(
        IErrorEntryRepository errorEntryRepository,
        IErrorClusterRepository clusterRepository,
        ILogger<FrequencyAnalyzer> logger)
    {
        _errorEntryRepository = errorEntryRepository;
        _clusterRepository = clusterRepository;
        _logger = logger;
    }

    public async Task<List<TrendDataPoint>> GetOccurrenceTimeSeriesAsync(string patternId, TimeSpan timeWindow, CancellationToken cancellationToken = default)
    {
        try
        {
            var endTime = DateTime.UtcNow;
            var startTime = endTime.Subtract(timeWindow);
            
            // Get all clusters associated with this pattern
            // Note: This is simplified - in a full implementation, you'd have a PatternCluster relationship table
            var dataPoints = new List<TrendDataPoint>();

            // Calculate hourly buckets
            var hours = (int)timeWindow.TotalHours;
            var bucketSize = Math.Max(1, hours / 24); // Limit to 24 data points max

            for (var time = startTime; time < endTime; time = time.AddHours(bucketSize))
            {
                if (cancellationToken.IsCancellationRequested)
                    break;

                var bucketEnd = time.AddHours(bucketSize);
                var count = await _errorEntryRepository.GetErrorCountByHourAsync(time, cancellationToken);
                
                // For this simplified version, we'll get user count as well
                var errors = await _errorEntryRepository.GetByTimeRangeAsync(time, bucketEnd, cancellationToken);
                var userCount = errors.Select(e => e.UserId).Where(u => !string.IsNullOrEmpty(u)).Distinct().Count();

                dataPoints.Add(new TrendDataPoint
                {
                    Timestamp = time,
                    Count = count,
                    UserCount = userCount
                });
            }

            _logger.LogDebug("Generated {Count} data points for pattern {PatternId} over {TimeWindow}", 
                dataPoints.Count, patternId, timeWindow);

            return dataPoints;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting occurrence time series for pattern {PatternId}", patternId);
            return new List<TrendDataPoint>();
        }
    }

    public async Task<double> CalculateOccurrenceRateAsync(string patternId, TimeSpan timeWindow, CancellationToken cancellationToken = default)
    {
        try
        {
            var dataPoints = await GetOccurrenceTimeSeriesAsync(patternId, timeWindow, cancellationToken);
            
            if (!dataPoints.Any())
                return 0.0;

            var totalOccurrences = dataPoints.Sum(dp => dp.Count);
            var totalHours = timeWindow.TotalHours;

            var rate = totalOccurrences / totalHours;
            
            _logger.LogDebug("Calculated occurrence rate for pattern {PatternId}: {Rate} errors/hour", patternId, rate);
            return rate;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calculating occurrence rate for pattern {PatternId}", patternId);
            return 0.0;
        }
    }

    public async Task<Dictionary<string, int>> GetHourlyDistributionAsync(string patternId, CancellationToken cancellationToken = default)
    {
        try
        {
            var distribution = new Dictionary<string, int>();
            
            // Initialize all hours
            for (int hour = 0; hour < 24; hour++)
            {
                distribution[hour.ToString("D2")] = 0;
            }

            // Get data for the last 7 days
            var endTime = DateTime.UtcNow;
            var startTime = endTime.AddDays(-7);
            
            var errors = await _errorEntryRepository.GetByTimeRangeAsync(startTime, endTime, cancellationToken);
            
            foreach (var error in errors)
            {
                var hour = error.Timestamp.Hour.ToString("D2");
                if (distribution.ContainsKey(hour))
                {
                    distribution[hour]++;
                }
            }

            _logger.LogDebug("Generated hourly distribution for pattern {PatternId}", patternId);
            return distribution;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting hourly distribution for pattern {PatternId}", patternId);
            return new Dictionary<string, int>();
        }
    }

    public async Task<Dictionary<DayOfWeek, int>> GetWeeklyDistributionAsync(string patternId, CancellationToken cancellationToken = default)
    {
        try
        {
            var distribution = new Dictionary<DayOfWeek, int>();
            
            // Initialize all days
            foreach (DayOfWeek day in Enum.GetValues<DayOfWeek>())
            {
                distribution[day] = 0;
            }

            // Get data for the last 4 weeks
            var endTime = DateTime.UtcNow;
            var startTime = endTime.AddDays(-28);
            
            var errors = await _errorEntryRepository.GetByTimeRangeAsync(startTime, endTime, cancellationToken);
            
            foreach (var error in errors)
            {
                var dayOfWeek = error.Timestamp.DayOfWeek;
                distribution[dayOfWeek]++;
            }

            _logger.LogDebug("Generated weekly distribution for pattern {PatternId}", patternId);
            return distribution;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting weekly distribution for pattern {PatternId}", patternId);
            return new Dictionary<DayOfWeek, int>();
        }
    }
}