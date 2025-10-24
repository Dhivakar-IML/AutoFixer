using MongoDB.Driver;
using MongoDB.Bson;
using AutoFixer.Models;

namespace AutoFixer.Data.Repositories;

/// <summary>
/// Repository implementation for ErrorEntry operations
/// </summary>
public class ErrorEntryRepository : Repository<ErrorEntry>, IErrorEntryRepository
{
    public ErrorEntryRepository(IMongoDatabase database, string collectionName = "ErrorEntries") 
        : base(database, collectionName)
    {
        // Create indexes for better performance
        CreateIndexes();
    }

    public async Task<IEnumerable<ErrorEntry>> GetByTimeRangeAsync(DateTime start, DateTime end, CancellationToken cancellationToken = default)
    {
        var filter = Builders<ErrorEntry>.Filter.And(
            Builders<ErrorEntry>.Filter.Gte(x => x.Timestamp, start),
            Builders<ErrorEntry>.Filter.Lte(x => x.Timestamp, end)
        );
        return await FindAsync(filter, cancellationToken);
    }

    public async Task<IEnumerable<ErrorEntry>> GetBySourceAsync(string source, CancellationToken cancellationToken = default)
    {
        var filter = Builders<ErrorEntry>.Filter.Eq(x => x.Source, source);
        return await FindAsync(filter, cancellationToken);
    }

    public async Task<IEnumerable<ErrorEntry>> GetUnclusteredAsync(CancellationToken cancellationToken = default)
    {
        var filter = Builders<ErrorEntry>.Filter.Or(
            Builders<ErrorEntry>.Filter.Eq(x => x.ClusterId, null),
            Builders<ErrorEntry>.Filter.Eq(x => x.ClusterId, "")
        );
        return await FindAsync(filter, cancellationToken);
    }

    public async Task<IEnumerable<ErrorEntry>> GetByClusterIdAsync(string clusterId, CancellationToken cancellationToken = default)
    {
        var filter = Builders<ErrorEntry>.Filter.Eq(x => x.ClusterId, clusterId);
        return await FindAsync(filter, cancellationToken);
    }

    public async Task<int> GetErrorCountByHourAsync(DateTime hour, CancellationToken cancellationToken = default)
    {
        var start = hour.Date.AddHours(hour.Hour);
        var end = start.AddHours(1);
        
        var filter = Builders<ErrorEntry>.Filter.And(
            Builders<ErrorEntry>.Filter.Gte(x => x.Timestamp, start),
            Builders<ErrorEntry>.Filter.Lt(x => x.Timestamp, end)
        );
        
        return (int)await CountAsync(filter, cancellationToken);
    }

    public async Task<IEnumerable<ErrorEntry>> GetRecentErrorsAsync(int count, CancellationToken cancellationToken = default)
    {
        return await _collection.Find(Builders<ErrorEntry>.Filter.Empty)
            .SortByDescending(x => x.Timestamp)
            .Limit(count)
            .ToListAsync(cancellationToken);
    }

    private void CreateIndexes()
    {
        var indexKeysDefinition = Builders<ErrorEntry>.IndexKeys
            .Ascending(x => x.Timestamp)
            .Ascending(x => x.Source)
            .Ascending(x => x.ClusterId);
        
        var indexModel = new CreateIndexModel<ErrorEntry>(indexKeysDefinition);
        _collection.Indexes.CreateOne(indexModel);

        // Index for unclustered errors
        var unclusteredIndex = Builders<ErrorEntry>.IndexKeys.Ascending(x => x.ClusterId);
        var unclusteredModel = new CreateIndexModel<ErrorEntry>(unclusteredIndex);
        _collection.Indexes.CreateOne(unclusteredModel);

        // Index for exception type
        var exceptionTypeIndex = Builders<ErrorEntry>.IndexKeys.Ascending(x => x.ExceptionType);
        var exceptionTypeModel = new CreateIndexModel<ErrorEntry>(exceptionTypeIndex);
        _collection.Indexes.CreateOne(exceptionTypeModel);
    }
}