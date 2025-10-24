using AutoFixer.Models;
using AutoFixer.Services.Interfaces;
using AutoFixer.Data.Repositories;

namespace AutoFixer.Services;

/// <summary>
/// Service for analyzing trends in error patterns
/// </summary>
public class TrendAnalyzer : ITrendAnalyzer
{
    private readonly IFrequencyAnalyzer _frequencyAnalyzer;
    private readonly ILogger<TrendAnalyzer> _logger;

    public TrendAnalyzer(IFrequencyAnalyzer frequencyAnalyzer, ILogger<TrendAnalyzer> logger)
    {
        _frequencyAnalyzer = frequencyAnalyzer;
        _logger = logger;
    }

    public async Task<PatternTrend> AnalyzeTrendAsync(ErrorPattern pattern, TimeSpan timeWindow, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("Analyzing trend for pattern {PatternId} over {TimeWindow}", pattern.Id, timeWindow);

            var dataPoints = await _frequencyAnalyzer.GetOccurrenceTimeSeriesAsync(pattern.Id, timeWindow, cancellationToken);
            
            var trendDirection = await CalculateTrendDirectionAsync(dataPoints, cancellationToken);
            var changeRate = await CalculateChangeRateAsync(dataPoints, cancellationToken);
            var isAccelerating = await IsAcceleratingAsync(dataPoints, cancellationToken);
            
            var trend = new PatternTrend
            {
                PatternId = pattern.Id,
                Direction = trendDirection > 0.1 ? TrendDirection.Increasing : 
                           trendDirection < -0.1 ? TrendDirection.Decreasing : 
                           TrendDirection.Stable,
                ChangeRate = changeRate,
                IsAccelerating = isAccelerating,
                TimeWindow = timeWindow,
                DataPoints = dataPoints,
                AnalysisDate = DateTime.UtcNow
            };

            // Generate forecast if we have enough data points
            if (dataPoints.Count >= 5)
            {
                trend.Forecast = await ForecastNextPeriodAsync(dataPoints, TimeSpan.FromHours(24), cancellationToken);
            }

            _logger.LogDebug("Trend analysis completed for pattern {PatternId}: Direction={Direction}, ChangeRate={ChangeRate}", 
                pattern.Id, trend.Direction, trend.ChangeRate);

            return trend;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error analyzing trend for pattern {PatternId}", pattern.Id);
            throw;
        }
    }

    public async Task<double> CalculateTrendDirectionAsync(string patternId, TimeSpan timeWindow, CancellationToken cancellationToken = default)
    {
        try
        {
            var dataPoints = await _frequencyAnalyzer.GetOccurrenceTimeSeriesAsync(patternId, timeWindow, cancellationToken);
            return await CalculateTrendDirectionAsync(dataPoints, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calculating trend direction for pattern {PatternId}", patternId);
            return 0.0;
        }
    }

    public async Task<double> CalculateChangeRateAsync(string patternId, TimeSpan timeWindow, CancellationToken cancellationToken = default)
    {
        try
        {
            var dataPoints = await _frequencyAnalyzer.GetOccurrenceTimeSeriesAsync(patternId, timeWindow, cancellationToken);
            return await CalculateChangeRateAsync(dataPoints, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calculating change rate for pattern {PatternId}", patternId);
            return 0.0;
        }
    }

    public async Task<bool> IsAcceleratingAsync(string patternId, TimeSpan timeWindow, CancellationToken cancellationToken = default)
    {
        try
        {
            var dataPoints = await _frequencyAnalyzer.GetOccurrenceTimeSeriesAsync(patternId, timeWindow, cancellationToken);
            return await IsAcceleratingAsync(dataPoints, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking acceleration for pattern {PatternId}", patternId);
            return false;
        }
    }

    public async Task<double> ForecastNextPeriodAsync(string patternId, TimeSpan analysisWindow, TimeSpan forecastPeriod, CancellationToken cancellationToken = default)
    {
        try
        {
            var dataPoints = await _frequencyAnalyzer.GetOccurrenceTimeSeriesAsync(patternId, analysisWindow, cancellationToken);
            var forecast = await ForecastNextPeriodAsync(dataPoints, forecastPeriod, cancellationToken);
            return forecast?.PredictedOccurrences ?? 0.0;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error forecasting next period for pattern {PatternId}", patternId);
            return 0.0;
        }
    }

    private async Task<double> CalculateTrendDirectionAsync(List<TrendDataPoint> dataPoints, CancellationToken cancellationToken = default)
    {
        if (dataPoints.Count < 2)
            return 0.0;

        try
        {
            // Simple linear regression to determine trend direction
            var n = dataPoints.Count;
            var sumX = 0.0;
            var sumY = 0.0;
            var sumXY = 0.0;
            var sumX2 = 0.0;

            for (int i = 0; i < n; i++)
            {
                var x = i; // Time index
                var y = dataPoints[i].Count; // Error count
                sumX += x;
                sumY += y;
                sumXY += x * y;
                sumX2 += x * x;
            }

            var slope = (n * sumXY - sumX * sumY) / (n * sumX2 - sumX * sumX);

            await Task.CompletedTask; // Make method async

            // Return normalized slope
            return slope;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calculating trend direction");
            return 0.0;
        }
    }

    public async Task<double> CalculateChangeRateAsync(List<TrendDataPoint> dataPoints, CancellationToken cancellationToken = default)
    {
        if (dataPoints.Count < 2)
            return 0.0;

        try
        {
            // Calculate percentage change from first to last data point
            var firstPeriodCount = dataPoints.Take(dataPoints.Count / 2).Sum(dp => dp.Count);
            var lastPeriodCount = dataPoints.Skip(dataPoints.Count / 2).Sum(dp => dp.Count);

            if (firstPeriodCount == 0)
                return lastPeriodCount > 0 ? 100.0 : 0.0;

            var changeRate = ((double)(lastPeriodCount - firstPeriodCount) / firstPeriodCount) * 100.0;
            
            await Task.CompletedTask; // Make method async
            return changeRate;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calculating change rate");
            return 0.0;
        }
    }

    public async Task<bool> IsAcceleratingAsync(List<TrendDataPoint> dataPoints, CancellationToken cancellationToken = default)
    {
        if (dataPoints.Count < 4)
            return false;

        try
        {
            // Compare the rate of change in the first half vs second half
            var midPoint = dataPoints.Count / 2;
            var firstHalf = dataPoints.Take(midPoint).ToList();
            var secondHalf = dataPoints.Skip(midPoint).ToList();

            var firstHalfRate = await CalculateChangeRateAsync(firstHalf, cancellationToken);
            var secondHalfRate = await CalculateChangeRateAsync(secondHalf, cancellationToken);

            // Acceleration if the rate of change is increasing
            return Math.Abs(secondHalfRate) > Math.Abs(firstHalfRate) * 1.2; // 20% increase in rate
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking if trend is accelerating");
            return false;
        }
    }

    public async Task<PatternForecast?> ForecastNextPeriodAsync(List<TrendDataPoint> dataPoints, TimeSpan forecastPeriod, CancellationToken cancellationToken = default)
    {
        if (dataPoints.Count < 3)
            return null;

        try
        {
            // Simple linear extrapolation for forecasting
            var direction = await CalculateTrendDirectionAsync(dataPoints, cancellationToken);
            var changeRate = await CalculateChangeRateAsync(dataPoints, cancellationToken);

            var lastCount = dataPoints.Last().Count;
            var averageCount = dataPoints.Average(dp => dp.Count);

            // Apply trend to forecast
            var trendDirection = direction > 0.1 ? TrendDirection.Increasing : 
                               direction < -0.1 ? TrendDirection.Decreasing : 
                               TrendDirection.Stable;
                               
            var forecastMultiplier = trendDirection switch
            {
                TrendDirection.Increasing => 1.0 + (Math.Abs(changeRate) / 100.0),
                TrendDirection.Decreasing => 1.0 - (Math.Abs(changeRate) / 100.0),
                _ => 1.0
            };

            var predictedOccurrences = (int)(averageCount * forecastMultiplier);

            // Calculate confidence based on data consistency
            var variance = dataPoints.Select(dp => Math.Pow(dp.Count - averageCount, 2)).Average();
            var standardDeviation = Math.Sqrt(variance);
            var confidence = Math.Max(0.1, Math.Min(0.9, 1.0 - (standardDeviation / averageCount)));

            return new PatternForecast
            {
                PredictedOccurrences = Math.Max(0, predictedOccurrences),
                Confidence = confidence,
                ForecastPeriod = forecastPeriod,
                ModelUsed = "Linear Extrapolation"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error forecasting next period");
            return null;
        }
    }
}