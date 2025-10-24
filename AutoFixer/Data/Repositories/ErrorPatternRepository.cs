using MongoDB.Driver;
using MongoDB.Bson;
using AutoFixer.Models;

namespace AutoFixer.Data.Repositories;

/// <summary>
/// Repository implementation for ErrorPattern operations
/// </summary>
public class ErrorPatternRepository : Repository<ErrorPattern>, IErrorPatternRepository
{
    public ErrorPatternRepository(IMongoDatabase database, string collectionName = "ErrorPatterns") 
        : base(database, collectionName)
    {
        CreateIndexes();
    }

    public async Task<IEnumerable<ErrorPattern>> GetActiveAsync(CancellationToken cancellationToken = default)
    {
        var filter = Builders<ErrorPattern>.Filter.Eq(x => x.Status, PatternStatus.Active);
        return await FindAsync(filter, cancellationToken);
    }

    public async Task<IEnumerable<ErrorPattern>> GetByTypeAsync(PatternType type, CancellationToken cancellationToken = default)
    {
        var filter = Builders<ErrorPattern>.Filter.Eq(x => x.Type, type);
        return await FindAsync(filter, cancellationToken);
    }

    public async Task<IEnumerable<ErrorPattern>> GetByPriorityAsync(PatternPriority priority, CancellationToken cancellationToken = default)
    {
        var filter = Builders<ErrorPattern>.Filter.Eq(x => x.Priority, priority);
        return await FindAsync(filter, cancellationToken);
    }

    public async Task<IEnumerable<ErrorPattern>> GetTrendingPatternsAsync(CancellationToken cancellationToken = default)
    {
        var filter = Builders<ErrorPattern>.Filter.And(
            Builders<ErrorPattern>.Filter.Eq(x => x.TrendDirection, TrendDirection.Increasing),
            Builders<ErrorPattern>.Filter.Ne(x => x.Status, PatternStatus.Resolved)
        );
        return await FindAsync(filter, cancellationToken);
    }

    public async Task<IEnumerable<ErrorPattern>> GetUnresolvedPatternsAsync(CancellationToken cancellationToken = default)
    {
        var filter = Builders<ErrorPattern>.Filter.And(
            Builders<ErrorPattern>.Filter.Ne(x => x.Status, PatternStatus.Resolved),
            Builders<ErrorPattern>.Filter.Ne(x => x.Status, PatternStatus.Ignored),
            Builders<ErrorPattern>.Filter.Ne(x => x.Status, PatternStatus.Archived)
        );
        return await FindAsync(filter, cancellationToken);
    }

    public async Task<bool> UpdateStatusAsync(string patternId, PatternStatus status, CancellationToken cancellationToken = default)
    {
        var filter = Builders<ErrorPattern>.Filter.Eq("_id", ObjectId.Parse(patternId));
        var update = Builders<ErrorPattern>.Update.Set(x => x.Status, status);
        
        var result = await _collection.UpdateOneAsync(filter, update, cancellationToken: cancellationToken);
        return result.ModifiedCount > 0;
    }

    private void CreateIndexes()
    {
        // Index for status
        var statusIndex = Builders<ErrorPattern>.IndexKeys.Ascending(x => x.Status);
        var statusModel = new CreateIndexModel<ErrorPattern>(statusIndex);
        _collection.Indexes.CreateOne(statusModel);

        // Index for type and priority
        var typePriorityIndex = Builders<ErrorPattern>.IndexKeys
            .Ascending(x => x.Type)
            .Ascending(x => x.Priority);
        var typePriorityModel = new CreateIndexModel<ErrorPattern>(typePriorityIndex);
        _collection.Indexes.CreateOne(typePriorityModel);

        // Index for trend direction
        var trendIndex = Builders<ErrorPattern>.IndexKeys.Ascending(x => x.TrendDirection);
        var trendModel = new CreateIndexModel<ErrorPattern>(trendIndex);
        _collection.Indexes.CreateOne(trendModel);

        // Index for confidence score
        var confidenceIndex = Builders<ErrorPattern>.IndexKeys.Descending(x => x.Confidence);
        var confidenceModel = new CreateIndexModel<ErrorPattern>(confidenceIndex);
        _collection.Indexes.CreateOne(confidenceModel);

        // Index for identified date
        var dateIndex = Builders<ErrorPattern>.IndexKeys.Descending(x => x.IdentifiedAt);
        var dateModel = new CreateIndexModel<ErrorPattern>(dateIndex);
        _collection.Indexes.CreateOne(dateModel);
    }
}