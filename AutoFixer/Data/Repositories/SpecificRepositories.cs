using MongoDB.Driver;
using MongoDB.Bson;
using AutoFixer.Models;

namespace AutoFixer.Data.Repositories;

/// <summary>
/// Repository implementation for RootCauseAnalysis operations
/// </summary>
public class RootCauseAnalysisRepository : Repository<RootCauseAnalysis>, IRootCauseAnalysisRepository
{
    public RootCauseAnalysisRepository(IMongoDatabase database, string collectionName = "RootCauseAnalyses") 
        : base(database, collectionName)
    {
        CreateIndexes();
    }

    public async Task<RootCauseAnalysis?> GetByPatternIdAsync(string patternId, CancellationToken cancellationToken = default)
    {
        var filter = Builders<RootCauseAnalysis>.Filter.Eq(x => x.PatternId, patternId);
        return await _collection.Find(filter).FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<IEnumerable<RootCauseAnalysis>> GetByConfidenceThresholdAsync(double threshold, CancellationToken cancellationToken = default)
    {
        var filter = Builders<RootCauseAnalysis>.Filter.ElemMatch(x => x.Hypotheses, 
            Builders<RootCauseHypothesis>.Filter.Gte(h => h.Confidence, threshold));
        return await FindAsync(filter, cancellationToken);
    }

    public async Task<bool> UpdateAnalysisAsync(string patternId, RootCauseAnalysis analysis, CancellationToken cancellationToken = default)
    {
        var filter = Builders<RootCauseAnalysis>.Filter.Eq(x => x.PatternId, patternId);
        analysis.UpdatedAt = DateTime.UtcNow;
        
        var options = new ReplaceOptions { IsUpsert = true };
        var result = await _collection.ReplaceOneAsync(filter, analysis, options, cancellationToken);
        return result.ModifiedCount > 0 || result.UpsertedId != null;
    }

    private void CreateIndexes()
    {
        // Unique index for pattern ID
        var patternIdIndex = Builders<RootCauseAnalysis>.IndexKeys.Ascending(x => x.PatternId);
        var patternIdModel = new CreateIndexModel<RootCauseAnalysis>(patternIdIndex, 
            new CreateIndexOptions { Unique = true });
        _collection.Indexes.CreateOne(patternIdModel);

        // Index for created date
        var dateIndex = Builders<RootCauseAnalysis>.IndexKeys.Descending(x => x.CreatedAt);
        var dateModel = new CreateIndexModel<RootCauseAnalysis>(dateIndex);
        _collection.Indexes.CreateOne(dateModel);
    }
}

/// <summary>
/// Repository implementation for PatternResolution operations
/// </summary>
public class PatternResolutionRepository : Repository<PatternResolution>, IPatternResolutionRepository
{
    public PatternResolutionRepository(IMongoDatabase database, string collectionName = "PatternResolutions") 
        : base(database, collectionName)
    {
        CreateIndexes();
    }

    public async Task<PatternResolution?> GetByPatternIdAsync(string patternId, CancellationToken cancellationToken = default)
    {
        var filter = Builders<PatternResolution>.Filter.Eq(x => x.PatternId, patternId);
        return await _collection.Find(filter).FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<IEnumerable<PatternResolution>> GetByResolverAsync(string resolver, CancellationToken cancellationToken = default)
    {
        var filter = Builders<PatternResolution>.Filter.Eq(x => x.ResolvedBy, resolver);
        return await FindAsync(filter, cancellationToken);
    }

    public async Task<IEnumerable<PatternResolution>> GetByEffectivenessAsync(double minEffectiveness, CancellationToken cancellationToken = default)
    {
        var filter = Builders<PatternResolution>.Filter.Gte(x => x.Effectiveness, minEffectiveness);
        return await FindAsync(filter, cancellationToken);
    }

    public async Task<double> GetAverageResolutionTimeAsync(CancellationToken cancellationToken = default)
    {
        var pipeline = new[]
        {
            new BsonDocument("$group", new BsonDocument
            {
                { "_id", BsonNull.Value },
                { "averageTimeToResolve", new BsonDocument("$avg", 
                    new BsonDocument("$toDouble", "$timeToResolve")) }
            })
        };

        var result = await _collection.Aggregate<BsonDocument>(pipeline).FirstOrDefaultAsync(cancellationToken);
        return result?["averageTimeToResolve"]?.AsDouble ?? 0.0;
    }

    private void CreateIndexes()
    {
        // Unique index for pattern ID
        var patternIdIndex = Builders<PatternResolution>.IndexKeys.Ascending(x => x.PatternId);
        var patternIdModel = new CreateIndexModel<PatternResolution>(patternIdIndex, 
            new CreateIndexOptions { Unique = true });
        _collection.Indexes.CreateOne(patternIdModel);

        // Index for resolver
        var resolverIndex = Builders<PatternResolution>.IndexKeys.Ascending(x => x.ResolvedBy);
        var resolverModel = new CreateIndexModel<PatternResolution>(resolverIndex);
        _collection.Indexes.CreateOne(resolverModel);

        // Index for effectiveness
        var effectivenessIndex = Builders<PatternResolution>.IndexKeys.Descending(x => x.Effectiveness);
        var effectivenessModel = new CreateIndexModel<PatternResolution>(effectivenessIndex);
        _collection.Indexes.CreateOne(effectivenessModel);

        // Index for resolved date
        var dateIndex = Builders<PatternResolution>.IndexKeys.Descending(x => x.ResolvedAt);
        var dateModel = new CreateIndexModel<PatternResolution>(dateIndex);
        _collection.Indexes.CreateOne(dateModel);
    }
}

/// <summary>
/// Repository implementation for PatternAlert operations
/// </summary>
public class PatternAlertRepository : Repository<PatternAlert>, IPatternAlertRepository
{
    public PatternAlertRepository(IMongoDatabase database, string collectionName = "PatternAlerts") 
        : base(database, collectionName)
    {
        CreateIndexes();
    }

    public async Task<IEnumerable<PatternAlert>> GetUnacknowledgedAlertsAsync(CancellationToken cancellationToken = default)
    {
        var filter = Builders<PatternAlert>.Filter.Eq(x => x.Acknowledged, false);
        return await FindAsync(filter, cancellationToken);
    }

    public async Task<IEnumerable<PatternAlert>> GetByPatternIdAsync(string patternId, CancellationToken cancellationToken = default)
    {
        var filter = Builders<PatternAlert>.Filter.Eq(x => x.PatternId, patternId);
        return await FindAsync(filter, cancellationToken);
    }

    public async Task<IEnumerable<PatternAlert>> GetBySeverityAsync(AlertSeverity severity, CancellationToken cancellationToken = default)
    {
        var filter = Builders<PatternAlert>.Filter.Eq(x => x.Severity, severity);
        return await FindAsync(filter, cancellationToken);
    }

    public async Task<bool> AcknowledgeAlertAsync(string alertId, string acknowledgedBy, CancellationToken cancellationToken = default)
    {
        var filter = Builders<PatternAlert>.Filter.Eq("_id", ObjectId.Parse(alertId));
        var update = Builders<PatternAlert>.Update
            .Set(x => x.Acknowledged, true)
            .Set(x => x.AcknowledgedBy, acknowledgedBy)
            .Set(x => x.AcknowledgedAt, DateTime.UtcNow);
        
        var result = await _collection.UpdateOneAsync(filter, update, cancellationToken: cancellationToken);
        return result.ModifiedCount > 0;
    }

    public async Task<IEnumerable<PatternAlert>> GetRecentAlertsAsync(int count, CancellationToken cancellationToken = default)
    {
        return await _collection.Find(Builders<PatternAlert>.Filter.Empty)
            .SortByDescending(x => x.SentAt)
            .Limit(count)
            .ToListAsync(cancellationToken);
    }

    private void CreateIndexes()
    {
        // Index for pattern ID
        var patternIdIndex = Builders<PatternAlert>.IndexKeys.Ascending(x => x.PatternId);
        var patternIdModel = new CreateIndexModel<PatternAlert>(patternIdIndex);
        _collection.Indexes.CreateOne(patternIdIndex);

        // Index for acknowledged status
        var acknowledgedIndex = Builders<PatternAlert>.IndexKeys.Ascending(x => x.Acknowledged);
        var acknowledgedModel = new CreateIndexModel<PatternAlert>(acknowledgedIndex);
        _collection.Indexes.CreateOne(acknowledgedModel);

        // Index for severity
        var severityIndex = Builders<PatternAlert>.IndexKeys.Ascending(x => x.Severity);
        var severityModel = new CreateIndexModel<PatternAlert>(severityIndex);
        _collection.Indexes.CreateOne(severityModel);

        // Index for sent date
        var dateIndex = Builders<PatternAlert>.IndexKeys.Descending(x => x.SentAt);
        var dateModel = new CreateIndexModel<PatternAlert>(dateIndex);
        _collection.Indexes.CreateOne(dateModel);
    }

    public async Task<List<PatternAlert>> GetAlertsAsync(
        AlertStatus? status = null, 
        AlertSeverity? severity = null, 
        int? timeframe = null, 
        bool? acknowledged = null, 
        CancellationToken cancellationToken = default)
    {
        var filterBuilder = Builders<PatternAlert>.Filter;
        var filters = new List<FilterDefinition<PatternAlert>>();

        if (status.HasValue)
        {
            filters.Add(filterBuilder.Eq(a => a.Status, status.Value));
        }

        if (severity.HasValue)
        {
            filters.Add(filterBuilder.Eq(a => a.Severity, severity.Value));
        }

        if (acknowledged.HasValue)
        {
            if (acknowledged.Value)
            {
                filters.Add(filterBuilder.Ne(a => a.AcknowledgedAt, null));
            }
            else
            {
                filters.Add(filterBuilder.Eq(a => a.AcknowledgedAt, null));
            }
        }

        if (timeframe.HasValue)
        {
            var cutoffTime = DateTime.UtcNow.AddHours(-timeframe.Value);
            filters.Add(filterBuilder.Gte(a => a.CreatedAt, cutoffTime));
        }

        var combinedFilter = filters.Any() ? filterBuilder.And(filters) : filterBuilder.Empty;

        return await _collection
            .Find(combinedFilter)
            .SortByDescending(a => a.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<List<PatternAlert>> GetAlertsFromTimeAsync(DateTime cutoffTime, CancellationToken cancellationToken = default)
    {
        var filter = Builders<PatternAlert>.Filter.Gte(a => a.CreatedAt, cutoffTime);
        return await _collection
            .Find(filter)
            .SortByDescending(a => a.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task UpdateAsync(string id, Dictionary<string, object> updates, CancellationToken cancellationToken = default)
    {
        var updateDefinition = Builders<PatternAlert>.Update.Combine(
            updates.Select(kvp => Builders<PatternAlert>.Update.Set(kvp.Key, kvp.Value))
        );

        await _collection.UpdateOneAsync(
            a => a.Id == id,
            updateDefinition,
            null,
            cancellationToken);
    }
}