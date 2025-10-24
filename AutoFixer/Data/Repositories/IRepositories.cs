using MongoDB.Driver;
using AutoFixer.Models;

namespace AutoFixer.Data.Repositories;

/// <summary>
/// Generic repository interface for MongoDB operations
/// </summary>
/// <typeparam name="T">Entity type</typeparam>
public interface IRepository<T> where T : class
{
    Task<T?> GetByIdAsync(string id, CancellationToken cancellationToken = default);
    Task<IEnumerable<T>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<IEnumerable<T>> FindAsync(FilterDefinition<T> filter, CancellationToken cancellationToken = default);
    Task<T> CreateAsync(T entity, CancellationToken cancellationToken = default);
    Task<bool> UpdateAsync(string id, T entity, CancellationToken cancellationToken = default);
    Task<bool> DeleteAsync(string id, CancellationToken cancellationToken = default);
    Task<long> CountAsync(FilterDefinition<T> filter, CancellationToken cancellationToken = default);
    Task<IEnumerable<T>> GetPagedAsync(FilterDefinition<T> filter, int page, int pageSize, CancellationToken cancellationToken = default);
}

/// <summary>
/// Specific repository interface for ErrorEntry operations
/// </summary>
public interface IErrorEntryRepository : IRepository<ErrorEntry>
{
    Task<IEnumerable<ErrorEntry>> GetByTimeRangeAsync(DateTime start, DateTime end, CancellationToken cancellationToken = default);
    Task<IEnumerable<ErrorEntry>> GetBySourceAsync(string source, CancellationToken cancellationToken = default);
    Task<IEnumerable<ErrorEntry>> GetUnclusteredAsync(CancellationToken cancellationToken = default);
    Task<IEnumerable<ErrorEntry>> GetByClusterIdAsync(string clusterId, CancellationToken cancellationToken = default);
    Task<int> GetErrorCountByHourAsync(DateTime hour, CancellationToken cancellationToken = default);
    Task<IEnumerable<ErrorEntry>> GetRecentErrorsAsync(int count, CancellationToken cancellationToken = default);
}

/// <summary>
/// Specific repository interface for ErrorCluster operations
/// </summary>
public interface IErrorClusterRepository : IRepository<ErrorCluster>
{
    Task<IEnumerable<ErrorCluster>> GetActiveClustersAsync(CancellationToken cancellationToken = default);
    Task<IEnumerable<ErrorCluster>> GetByPatternSignatureAsync(string patternSignature, CancellationToken cancellationToken = default);
    Task<IEnumerable<ErrorCluster>> GetByTimeRangeAsync(DateTime start, DateTime end, CancellationToken cancellationToken = default);
    Task<IEnumerable<ErrorCluster>> GetBySeverityAsync(ErrorSeverity severity, CancellationToken cancellationToken = default);
    Task<bool> UpdateOccurrencesAsync(string clusterId, int newCount, CancellationToken cancellationToken = default);
    Task<IEnumerable<ErrorCluster>> GetTopClustersByOccurrencesAsync(int count, CancellationToken cancellationToken = default);
    Task<IEnumerable<ErrorCluster>> GetRecentClustersWithoutPatternsAsync(TimeSpan timeWindow, CancellationToken cancellationToken = default);
}

/// <summary>
/// Specific repository interface for ErrorPattern operations
/// </summary>
public interface IErrorPatternRepository : IRepository<ErrorPattern>
{
    Task<IEnumerable<ErrorPattern>> GetActiveAsync(CancellationToken cancellationToken = default);
    Task<IEnumerable<ErrorPattern>> GetByTypeAsync(PatternType type, CancellationToken cancellationToken = default);
    Task<IEnumerable<ErrorPattern>> GetByPriorityAsync(PatternPriority priority, CancellationToken cancellationToken = default);
    Task<IEnumerable<ErrorPattern>> GetTrendingPatternsAsync(CancellationToken cancellationToken = default);
    Task<IEnumerable<ErrorPattern>> GetUnresolvedPatternsAsync(CancellationToken cancellationToken = default);
    Task<bool> UpdateStatusAsync(string patternId, PatternStatus status, CancellationToken cancellationToken = default);
}

/// <summary>
/// Specific repository interface for RootCauseAnalysis operations
/// </summary>
public interface IRootCauseAnalysisRepository : IRepository<RootCauseAnalysis>
{
    Task<RootCauseAnalysis?> GetByPatternIdAsync(string patternId, CancellationToken cancellationToken = default);
    Task<IEnumerable<RootCauseAnalysis>> GetByConfidenceThresholdAsync(double threshold, CancellationToken cancellationToken = default);
    Task<bool> UpdateAnalysisAsync(string patternId, RootCauseAnalysis analysis, CancellationToken cancellationToken = default);
}

/// <summary>
/// Specific repository interface for PatternResolution operations
/// </summary>
public interface IPatternResolutionRepository : IRepository<PatternResolution>
{
    Task<PatternResolution?> GetByPatternIdAsync(string patternId, CancellationToken cancellationToken = default);
    Task<IEnumerable<PatternResolution>> GetByResolverAsync(string resolver, CancellationToken cancellationToken = default);
    Task<IEnumerable<PatternResolution>> GetByEffectivenessAsync(double minEffectiveness, CancellationToken cancellationToken = default);
    Task<double> GetAverageResolutionTimeAsync(CancellationToken cancellationToken = default);
}

/// <summary>
/// Repository interface for PatternAlert operations
/// </summary>
public interface IPatternAlertRepository : IRepository<PatternAlert>
{
    Task<IEnumerable<PatternAlert>> GetUnacknowledgedAlertsAsync(CancellationToken cancellationToken = default);
    Task<IEnumerable<PatternAlert>> GetByPatternIdAsync(string patternId, CancellationToken cancellationToken = default);
    Task<IEnumerable<PatternAlert>> GetBySeverityAsync(AlertSeverity severity, CancellationToken cancellationToken = default);
    Task<bool> AcknowledgeAlertAsync(string alertId, string acknowledgedBy, CancellationToken cancellationToken = default);
    Task<IEnumerable<PatternAlert>> GetRecentAlertsAsync(int count, CancellationToken cancellationToken = default);
    Task<List<PatternAlert>> GetAlertsAsync(AlertStatus? status = null, AlertSeverity? severity = null, int? timeframe = null, bool? acknowledged = null, CancellationToken cancellationToken = default);
    Task<List<PatternAlert>> GetAlertsFromTimeAsync(DateTime cutoffTime, CancellationToken cancellationToken = default);
    Task UpdateAsync(string id, Dictionary<string, object> updates, CancellationToken cancellationToken = default);
}