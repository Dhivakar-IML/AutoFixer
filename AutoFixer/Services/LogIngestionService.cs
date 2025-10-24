using AutoFixer.Models;
using AutoFixer.Services.Interfaces;

namespace AutoFixer.Services;

/// <summary>
/// Main service that coordinates log ingestion from multiple sources
/// </summary>
public class LogIngestionService : ILogIngestionService
{
    private readonly ISeqLogIngestionService _seqService;
    private readonly INewRelicLogIngestionService _newRelicService;
    private readonly IMongoAuditLogIngestionService _mongoAuditService;
    private readonly ILogger<LogIngestionService> _logger;

    public LogIngestionService(
        ISeqLogIngestionService seqService,
        INewRelicLogIngestionService newRelicService,
        IMongoAuditLogIngestionService mongoAuditService,
        ILogger<LogIngestionService> logger)
    {
        _seqService = seqService;
        _newRelicService = newRelicService;
        _mongoAuditService = mongoAuditService;
        _logger = logger;
    }

    public async Task<IEnumerable<ErrorEntry>> IngestFromSeqAsync(DateTime since, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Starting Seq ingestion since {Since}", since);
            var errors = await _seqService.IngestErrorsAsync(since, cancellationToken);
            _logger.LogInformation("Completed Seq ingestion: {Count} errors", errors.Count());
            return errors;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to ingest from Seq");
            return Enumerable.Empty<ErrorEntry>();
        }
    }

    public async Task<IEnumerable<ErrorEntry>> IngestFromNewRelicAsync(DateTime since, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Starting New Relic ingestion since {Since}", since);
            var errors = await _newRelicService.IngestErrorsAsync(since, cancellationToken);
            _logger.LogInformation("Completed New Relic ingestion: {Count} errors", errors.Count());
            return errors;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to ingest from New Relic");
            return Enumerable.Empty<ErrorEntry>();
        }
    }

    public async Task<IEnumerable<ErrorEntry>> IngestFromMongoAuditAsync(DateTime since, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Starting MongoDB audit ingestion since {Since}", since);
            var errors = await _mongoAuditService.IngestErrorsAsync(since, cancellationToken);
            _logger.LogInformation("Completed MongoDB audit ingestion: {Count} errors", errors.Count());
            return errors;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to ingest from MongoDB audit logs");
            return Enumerable.Empty<ErrorEntry>();
        }
    }

    /// <summary>
    /// Ingests errors from all available sources and combines them
    /// </summary>
    public async Task<IEnumerable<ErrorEntry>> IngestFromAllSourcesAsync(DateTime since, CancellationToken cancellationToken = default)
    {
        var tasks = new List<Task<IEnumerable<ErrorEntry>>>
        {
            IngestFromSeqAsync(since, cancellationToken),
            IngestFromNewRelicAsync(since, cancellationToken),
            IngestFromMongoAuditAsync(since, cancellationToken)
        };

        try
        {
            var results = await Task.WhenAll(tasks);
            var allErrors = results.SelectMany(r => r).ToList();
            
            _logger.LogInformation("Total errors ingested from all sources: {Count}", allErrors.Count);
            return allErrors;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during multi-source ingestion");
            // Return successful results even if some sources failed
            var completedResults = tasks.Where(t => t.IsCompletedSuccessfully)
                                       .SelectMany(t => t.Result);
            return completedResults;
        }
    }

    /// <summary>
    /// Tests connectivity to all log sources
    /// </summary>
    public async Task<Dictionary<string, bool>> TestAllConnectionsAsync(CancellationToken cancellationToken = default)
    {
        var results = new Dictionary<string, bool>();

        try
        {
            results["Seq"] = await _seqService.TestConnectionAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to test Seq connection");
            results["Seq"] = false;
        }

        try
        {
            results["NewRelic"] = await _newRelicService.TestConnectionAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to test New Relic connection");
            results["NewRelic"] = false;
        }

        try
        {
            results["MongoAudit"] = await _mongoAuditService.TestConnectionAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to test MongoDB audit connection");
            results["MongoAudit"] = false;
        }

        return results;
    }
}