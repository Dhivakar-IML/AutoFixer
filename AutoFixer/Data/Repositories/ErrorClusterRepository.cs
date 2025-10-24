using MongoDB.Driver;
using MongoDB.Bson;
using AutoFixer.Models;

namespace AutoFixer.Data.Repositories;

/// <summary>
/// Repository implementation for ErrorCluster operations
/// </summary>
public class ErrorClusterRepository : Repository<ErrorCluster>, IErrorClusterRepository
{
    public ErrorClusterRepository(IMongoDatabase database, string collectionName = "ErrorClusters") 
        : base(database, collectionName)
    {
        CreateIndexes();
    }

    public async Task<IEnumerable<ErrorCluster>> GetActiveClustersAsync(CancellationToken cancellationToken = default)
    {
        var filter = Builders<ErrorCluster>.Filter.Ne(x => x.Status, ClusterStatus.Resolved);
        return await FindAsync(filter, cancellationToken);
    }

    public async Task<IEnumerable<ErrorCluster>> GetRecentClustersWithoutPatternsAsync(TimeSpan timeWindow, CancellationToken cancellationToken = default)
    {
        var cutoffTime = DateTime.UtcNow.Subtract(timeWindow);
        var filter = Builders<ErrorCluster>.Filter.And(
            Builders<ErrorCluster>.Filter.Gte(x => x.LastSeen, cutoffTime),
            // Assuming clusters without patterns have an empty or null PatternId field
            // You might need to adjust this based on your actual schema
            Builders<ErrorCluster>.Filter.Or(
                Builders<ErrorCluster>.Filter.Eq("PatternId", BsonNull.Value),
                Builders<ErrorCluster>.Filter.Eq("PatternId", ""),
                Builders<ErrorCluster>.Filter.Exists("PatternId", false)
            )
        );
        return await FindAsync(filter, cancellationToken);
    }

    public async Task<IEnumerable<ErrorCluster>> GetByPatternSignatureAsync(string patternSignature, CancellationToken cancellationToken = default)
    {
        var filter = Builders<ErrorCluster>.Filter.Eq(x => x.PatternSignature, patternSignature);
        return await FindAsync(filter, cancellationToken);
    }

    public async Task<IEnumerable<ErrorCluster>> GetByTimeRangeAsync(DateTime start, DateTime end, CancellationToken cancellationToken = default)
    {
        var filter = Builders<ErrorCluster>.Filter.And(
            Builders<ErrorCluster>.Filter.Gte(x => x.FirstSeen, start),
            Builders<ErrorCluster>.Filter.Lte(x => x.LastSeen, end)
        );
        return await FindAsync(filter, cancellationToken);
    }

    public async Task<IEnumerable<ErrorCluster>> GetBySeverityAsync(ErrorSeverity severity, CancellationToken cancellationToken = default)
    {
        var filter = Builders<ErrorCluster>.Filter.Eq(x => x.Severity, severity);
        return await FindAsync(filter, cancellationToken);
    }

    public async Task<bool> UpdateOccurrencesAsync(string clusterId, int newCount, CancellationToken cancellationToken = default)
    {
        var filter = Builders<ErrorCluster>.Filter.Eq("_id", ObjectId.Parse(clusterId));
        var update = Builders<ErrorCluster>.Update
            .Set(x => x.Occurrences, newCount)
            .Set(x => x.LastSeen, DateTime.UtcNow)
            .Set(x => x.UpdatedAt, DateTime.UtcNow);
        
        var result = await _collection.UpdateOneAsync(filter, update, cancellationToken: cancellationToken);
        return result.ModifiedCount > 0;
    }

    public async Task<IEnumerable<ErrorCluster>> GetTopClustersByOccurrencesAsync(int count, CancellationToken cancellationToken = default)
    {
        return await _collection.Find(Builders<ErrorCluster>.Filter.Empty)
            .SortByDescending(x => x.Occurrences)
            .Limit(count)
            .ToListAsync(cancellationToken);
    }

    private void CreateIndexes()
    {
        // Index for pattern signature
        var signatureIndex = Builders<ErrorCluster>.IndexKeys.Ascending(x => x.PatternSignature);
        var signatureModel = new CreateIndexModel<ErrorCluster>(signatureIndex);
        _collection.Indexes.CreateOne(signatureModel);

        // Index for time range queries
        var timeIndex = Builders<ErrorCluster>.IndexKeys
            .Ascending(x => x.FirstSeen)
            .Ascending(x => x.LastSeen);
        var timeModel = new CreateIndexModel<ErrorCluster>(timeIndex);
        _collection.Indexes.CreateOne(timeModel);

        // Index for severity and status
        var severityStatusIndex = Builders<ErrorCluster>.IndexKeys
            .Ascending(x => x.Severity)
            .Ascending(x => x.Status);
        var severityStatusModel = new CreateIndexModel<ErrorCluster>(severityStatusIndex);
        _collection.Indexes.CreateOne(severityStatusModel);

        // Index for occurrences (for top clusters)
        var occurrencesIndex = Builders<ErrorCluster>.IndexKeys.Descending(x => x.Occurrences);
        var occurrencesModel = new CreateIndexModel<ErrorCluster>(occurrencesIndex);
        _collection.Indexes.CreateOne(occurrencesModel);
    }
}