using Microsoft.ML;
using Microsoft.ML.Data;
using Microsoft.Extensions.Options;
using AutoFixer.Configuration;
using AutoFixer.Models;
using AutoFixer.Services.Interfaces;
using AutoFixer.Data.Repositories;

namespace AutoFixer.ML;

/// <summary>
/// ML.NET-based error clustering engine using TF-IDF and cosine similarity
/// </summary>
public class ErrorClusteringEngine : IErrorClusteringEngine
{
    private readonly MLContext _mlContext;
    private readonly MLSettings _settings;
    private readonly IErrorNormalizationService _normalizationService;
    private readonly IErrorClusterRepository _clusterRepository;
    private readonly ILogger<ErrorClusteringEngine> _logger;
    private ITransformer? _model;
    private readonly object _modelLock = new();

    public ErrorClusteringEngine(
        IOptions<MLSettings> settings,
        IErrorNormalizationService normalizationService,
        IErrorClusterRepository clusterRepository,
        ILogger<ErrorClusteringEngine> logger)
    {
        _mlContext = new MLContext(seed: 0);
        _settings = settings.Value;
        _normalizationService = normalizationService;
        _clusterRepository = clusterRepository;
        _logger = logger;
    }

    public async Task TrainAsync(IEnumerable<ErrorEntry> historicalErrors, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Starting ML model training with {Count} historical errors", historicalErrors.Count());

            // Prepare training data
            var trainingData = historicalErrors.Select(error => new ErrorMLData
            {
                ErrorText = _normalizationService.NormalizeError(error.Message, error.StackTrace),
                ExceptionType = error.ExceptionType ?? "Unknown",
                Source = error.Source,
                StatusCode = error.StatusCode?.ToString() ?? "0"
            }).ToList();

            if (!trainingData.Any())
            {
                _logger.LogWarning("No training data available");
                return;
            }

            var mlData = _mlContext.Data.LoadFromEnumerable(trainingData);

            // Build text featurization pipeline
            var pipeline = _mlContext.Transforms.Text.FeaturizeText(
                    outputColumnName: "ErrorTextFeatures",
                    inputColumnName: nameof(ErrorMLData.ErrorText))
                .Append(_mlContext.Transforms.Categorical.OneHotEncoding(
                    outputColumnName: "ExceptionTypeEncoded",
                    inputColumnName: nameof(ErrorMLData.ExceptionType)))
                .Append(_mlContext.Transforms.Categorical.OneHotEncoding(
                    outputColumnName: "SourceEncoded",
                    inputColumnName: nameof(ErrorMLData.Source)))
                .Append(_mlContext.Transforms.Categorical.OneHotEncoding(
                    outputColumnName: "StatusCodeEncoded",
                    inputColumnName: nameof(ErrorMLData.StatusCode)))
                .Append(_mlContext.Transforms.Concatenate(
                    outputColumnName: "Features",
                    "ErrorTextFeatures",
                    "ExceptionTypeEncoded",
                    "SourceEncoded",
                    "StatusCodeEncoded"));

            // Train the model
            lock (_modelLock)
            {
                _model = pipeline.Fit(mlData);
            }

            _logger.LogInformation("ML model training completed successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during ML model training");
            throw;
        }
    }

    public async Task<IEnumerable<ErrorCluster>> ClusterErrorsAsync(IEnumerable<ErrorEntry> errors, CancellationToken cancellationToken = default)
    {
        try
        {
            if (!IsModelTrainedAsync().Result)
            {
                _logger.LogWarning("Model not trained, cannot cluster errors");
                return Enumerable.Empty<ErrorCluster>();
            }

            var clusters = new List<ErrorCluster>();
            var processedErrors = new List<ErrorEntry>();

            foreach (var error in errors)
            {
                if (cancellationToken.IsCancellationRequested)
                    break;

                var existingCluster = await FindSimilarClusterAsync(error, cancellationToken);
                
                if (existingCluster != null)
                {
                    // Add to existing cluster
                    existingCluster.ErrorIds.Add(error.Id);
                    existingCluster.Occurrences++;
                    existingCluster.LastSeen = DateTime.UtcNow;
                    existingCluster.UpdatedAt = DateTime.UtcNow;

                    // Update affected users and endpoints
                    if (!string.IsNullOrEmpty(error.UserId) && !existingCluster.AffectedUsers.Contains(error.UserId))
                        existingCluster.AffectedUsers.Add(error.UserId);

                    if (!string.IsNullOrEmpty(error.Endpoint) && !existingCluster.AffectedEndpoints.Contains(error.Endpoint))
                        existingCluster.AffectedEndpoints.Add(error.Endpoint);

                    error.ClusterId = existingCluster.Id;
                }
                else
                {
                    // Create new cluster
                    var normalizedError = _normalizationService.NormalizeError(error.Message, error.StackTrace);
                    var newCluster = new ErrorCluster
                    {
                        PatternSignature = _normalizationService.GeneratePatternSignature(normalizedError),
                        RepresentativeError = error.Message,
                        ErrorIds = new List<string> { error.Id },
                        Occurrences = 1,
                        FirstSeen = error.Timestamp,
                        LastSeen = error.Timestamp,
                        Severity = error.Severity,
                        AffectedUsers = !string.IsNullOrEmpty(error.UserId) ? new List<string> { error.UserId } : new List<string>(),
                        AffectedEndpoints = !string.IsNullOrEmpty(error.Endpoint) ? new List<string> { error.Endpoint } : new List<string>(),
                        MlConfidenceScore = 1.0, // New cluster, high confidence
                        Status = ClusterStatus.Identified
                    };

                    clusters.Add(newCluster);
                    error.ClusterId = newCluster.Id;
                }

                processedErrors.Add(error);
            }

            _logger.LogInformation("Clustered {ErrorCount} errors into {ClusterCount} clusters", 
                processedErrors.Count, clusters.Count);

            return clusters;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during error clustering");
            throw;
        }
    }

    public async Task<ErrorCluster?> FindSimilarClusterAsync(ErrorEntry error, CancellationToken cancellationToken = default)
    {
        try
        {
            if (!IsModelTrainedAsync().Result)
                return null;

            var normalizedError = _normalizationService.NormalizeError(error.Message, error.StackTrace);
            var patternSignature = _normalizationService.GeneratePatternSignature(normalizedError);

            // First try exact pattern signature match
            var exactMatches = await _clusterRepository.GetByPatternSignatureAsync(patternSignature, cancellationToken);
            var exactMatch = exactMatches.FirstOrDefault();
            if (exactMatch != null)
            {
                _logger.LogDebug("Found exact pattern match for error {ErrorId}", error.Id);
                return exactMatch;
            }

            // If no exact match, use ML similarity
            var activeClusters = await _clusterRepository.GetActiveClustersAsync(cancellationToken);
            
            double bestSimilarity = 0;
            ErrorCluster? bestCluster = null;

            foreach (var cluster in activeClusters.Take(100)) // Limit to top 100 clusters for performance
            {
                if (cancellationToken.IsCancellationRequested)
                    break;

                // Create a representative error entry for the cluster
                var clusterRepresentative = new ErrorEntry
                {
                    Message = cluster.RepresentativeError,
                    ExceptionType = ExtractExceptionTypeFromCluster(cluster),
                    Source = "Cluster"
                };

                var similarity = await CalculateSimilarityAsync(error, clusterRepresentative, cancellationToken);
                
                if (similarity > bestSimilarity && similarity >= _settings.SimilarityThreshold)
                {
                    bestSimilarity = similarity;
                    bestCluster = cluster;
                }
            }

            if (bestCluster != null)
            {
                _logger.LogDebug("Found similar cluster for error {ErrorId} with similarity {Similarity}", 
                    error.Id, bestSimilarity);
            }

            return bestCluster;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error finding similar cluster for error {ErrorId}", error.Id);
            return null;
        }
    }

    public async Task<bool> IsModelTrainedAsync()
    {
        return await Task.FromResult(_model != null);
    }

    public async Task<double> CalculateSimilarityAsync(ErrorEntry error1, ErrorEntry error2, CancellationToken cancellationToken = default)
    {
        try
        {
            if (!IsModelTrainedAsync().Result)
                return 0.0;

            var features1 = TransformErrorToFeatures(error1);
            var features2 = TransformErrorToFeatures(error2);

            if (features1 == null || features2 == null)
                return 0.0;

            // Calculate cosine similarity between feature vectors
            var similarity = CalculateCosineSimilarity(features1.Features, features2.Features);
            return await Task.FromResult(similarity);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calculating similarity between errors");
            return 0.0;
        }
    }

    private ErrorMLPrediction? TransformErrorToFeatures(ErrorEntry error)
    {
        try
        {
            lock (_modelLock)
            {
                if (_model == null)
                    return null;

                var errorData = new ErrorMLData
                {
                    ErrorText = _normalizationService.NormalizeError(error.Message, error.StackTrace),
                    ExceptionType = error.ExceptionType ?? "Unknown",
                    Source = error.Source,
                    StatusCode = error.StatusCode?.ToString() ?? "0"
                };

                var predictionEngine = _mlContext.Model.CreatePredictionEngine<ErrorMLData, ErrorMLPrediction>(_model);
                return predictionEngine.Predict(errorData);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error transforming error to features");
            return null;
        }
    }

    private double CalculateCosineSimilarity(float[] vector1, float[] vector2)
    {
        if (vector1.Length != vector2.Length)
            return 0.0;

        double dotProduct = 0.0;
        double norm1 = 0.0;
        double norm2 = 0.0;

        for (int i = 0; i < vector1.Length; i++)
        {
            dotProduct += vector1[i] * vector2[i];
            norm1 += vector1[i] * vector1[i];
            norm2 += vector2[i] * vector2[i];
        }

        if (norm1 == 0.0 || norm2 == 0.0)
            return 0.0;

        return dotProduct / (Math.Sqrt(norm1) * Math.Sqrt(norm2));
    }

    private string ExtractExceptionTypeFromCluster(ErrorCluster cluster)
    {
        // Extract exception type from the representative error message
        // This is a simplified implementation
        var message = cluster.RepresentativeError;
        var commonExceptions = new[] { "ArgumentException", "NullReferenceException", "InvalidOperationException", "TimeoutException" };
        
        foreach (var exceptionType in commonExceptions)
        {
            if (message.Contains(exceptionType, StringComparison.OrdinalIgnoreCase))
                return exceptionType;
        }

        return "Unknown";
    }
}

/// <summary>
/// ML data model for training
/// </summary>
public class ErrorMLData
{
    public string ErrorText { get; set; } = string.Empty;
    public string ExceptionType { get; set; } = string.Empty;
    public string Source { get; set; } = string.Empty;
    public string StatusCode { get; set; } = string.Empty;
}

/// <summary>
/// ML prediction model
/// </summary>
public class ErrorMLPrediction
{
    [VectorType]
    public float[] Features { get; set; } = Array.Empty<float>();
}