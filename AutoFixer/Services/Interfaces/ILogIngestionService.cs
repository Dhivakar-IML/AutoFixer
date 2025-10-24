using AutoFixer.Models;

namespace AutoFixer.Services.Interfaces;

/// <summary>
/// Interface for log ingestion services
/// </summary>
public interface ILogIngestionService
{
    Task<IEnumerable<ErrorEntry>> IngestFromSeqAsync(DateTime since, CancellationToken cancellationToken = default);
    Task<IEnumerable<ErrorEntry>> IngestFromNewRelicAsync(DateTime since, CancellationToken cancellationToken = default);
    Task<IEnumerable<ErrorEntry>> IngestFromMongoAuditAsync(DateTime since, CancellationToken cancellationToken = default);
}

/// <summary>
/// Interface for Seq-specific log ingestion
/// </summary>
public interface ISeqLogIngestionService
{
    Task<IEnumerable<ErrorEntry>> IngestErrorsAsync(DateTime since, CancellationToken cancellationToken = default);
    Task<bool> TestConnectionAsync(CancellationToken cancellationToken = default);
}

/// <summary>
/// Interface for New Relic-specific log ingestion
/// </summary>
public interface INewRelicLogIngestionService
{
    Task<IEnumerable<ErrorEntry>> IngestErrorsAsync(DateTime since, CancellationToken cancellationToken = default);
    Task<bool> TestConnectionAsync(CancellationToken cancellationToken = default);
}

/// <summary>
/// Interface for MongoDB audit log ingestion
/// </summary>
public interface IMongoAuditLogIngestionService
{
    Task<IEnumerable<ErrorEntry>> IngestErrorsAsync(DateTime since, CancellationToken cancellationToken = default);
    Task<bool> TestConnectionAsync(CancellationToken cancellationToken = default);
}