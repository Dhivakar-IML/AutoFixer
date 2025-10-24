using AutoFixer.Models;

namespace AutoFixer.Services.Interfaces;

/// <summary>
/// Interface for error clustering using ML algorithms
/// </summary>
public interface IErrorClusteringEngine
{
    Task<IEnumerable<ErrorCluster>> ClusterErrorsAsync(IEnumerable<ErrorEntry> errors, CancellationToken cancellationToken = default);
    Task<ErrorCluster?> FindSimilarClusterAsync(ErrorEntry error, CancellationToken cancellationToken = default);
    Task TrainAsync(IEnumerable<ErrorEntry> historicalErrors, CancellationToken cancellationToken = default);
    Task<bool> IsModelTrainedAsync();
    Task<double> CalculateSimilarityAsync(ErrorEntry error1, ErrorEntry error2, CancellationToken cancellationToken = default);
}

/// <summary>
/// Interface for error text normalization and feature extraction
/// </summary>
public interface IErrorNormalizationService
{
    string NormalizeError(string message, string? stackTrace);
    List<string> ExtractKeyStackFrames(string? stackTrace);
    string GeneratePatternSignature(string normalizedError);
    ErrorFeatures ExtractFeatures(ErrorEntry error);
}

/// <summary>
/// Interface for anomaly detection in error patterns
/// </summary>
public interface IAnomalyDetector
{
    Task<bool> IsAnomalousAsync(ErrorCluster cluster, CancellationToken cancellationToken = default);
    Task<double> CalculateAnomalyScoreAsync(ErrorCluster cluster, CancellationToken cancellationToken = default);
    Task TrainBaselineAsync(IEnumerable<ErrorCluster> historicalClusters, CancellationToken cancellationToken = default);
}