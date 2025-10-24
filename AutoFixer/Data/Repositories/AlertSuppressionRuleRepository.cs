using AutoFixer.Data.Repositories.Interfaces;
using AutoFixer.Models;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;

namespace AutoFixer.Data.Repositories;

/// <summary>
/// MongoDB repository for alert suppression rules
/// </summary>
public class AlertSuppressionRuleRepository : IAlertSuppressionRuleRepository
{
    private readonly IMongoCollection<AlertSuppressionRule> _collection;
    private readonly ILogger<AlertSuppressionRuleRepository> _logger;

    public AlertSuppressionRuleRepository(IMongoDatabase database, ILogger<AlertSuppressionRuleRepository> logger)
    {
        _collection = database.GetCollection<AlertSuppressionRule>("alertSuppressionRules");
        _logger = logger;
        
        // Create indexes
        CreateIndexes();
    }

    /// <summary>
    /// Creates a new suppression rule
    /// </summary>
    public async Task CreateAsync(AlertSuppressionRule rule, CancellationToken cancellationToken = default)
    {
        try
        {
            await _collection.InsertOneAsync(rule, null, cancellationToken);
            _logger.LogInformation("Created alert suppression rule {RuleId}", rule.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating alert suppression rule {RuleId}", rule.Id);
            throw;
        }
    }

    /// <summary>
    /// Gets a suppression rule by ID
    /// </summary>
    public async Task<AlertSuppressionRule?> GetByIdAsync(string id, CancellationToken cancellationToken = default)
    {
        try
        {
            return await _collection.Find(r => r.Id == id).FirstOrDefaultAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting alert suppression rule {RuleId}", id);
            throw;
        }
    }

    /// <summary>
    /// Gets all suppression rules with optional filtering
    /// </summary>
    public async Task<List<AlertSuppressionRule>> GetAllAsync(bool? isActive = null, CancellationToken cancellationToken = default)
    {
        try
        {
            var filter = Builders<AlertSuppressionRule>.Filter.Empty;
            
            if (isActive.HasValue)
            {
                filter = Builders<AlertSuppressionRule>.Filter.Eq(r => r.IsActive, isActive.Value);
            }

            return await _collection
                .Find(filter)
                .SortByDescending(r => r.CreatedAt)
                .ToListAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting alert suppression rules");
            throw;
        }
    }

    /// <summary>
    /// Gets only active (non-expired) suppression rules
    /// </summary>
    public async Task<List<AlertSuppressionRule>> GetActiveRulesAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var now = DateTime.UtcNow;
            var filter = Builders<AlertSuppressionRule>.Filter.And(
                Builders<AlertSuppressionRule>.Filter.Eq(r => r.IsActive, true),
                Builders<AlertSuppressionRule>.Filter.Or(
                    Builders<AlertSuppressionRule>.Filter.Eq(r => r.ExpiresAt, null),
                    Builders<AlertSuppressionRule>.Filter.Gt(r => r.ExpiresAt, now)
                )
            );

            return await _collection
                .Find(filter)
                .SortByDescending(r => r.CreatedAt)
                .ToListAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting active alert suppression rules");
            throw;
        }
    }

    /// <summary>
    /// Updates a suppression rule
    /// </summary>
    public async Task UpdateAsync(string id, Dictionary<string, object> updates, CancellationToken cancellationToken = default)
    {
        try
        {
            var updateDefinition = Builders<AlertSuppressionRule>.Update.Combine(
                updates.Select(kvp => Builders<AlertSuppressionRule>.Update.Set(kvp.Key, kvp.Value))
            );

            var result = await _collection.UpdateOneAsync(
                r => r.Id == id,
                updateDefinition,
                null,
                cancellationToken);

            if (result.ModifiedCount == 0)
            {
                _logger.LogWarning("No alert suppression rule found with ID {RuleId} to update", id);
            }
            else
            {
                _logger.LogInformation("Updated alert suppression rule {RuleId}", id);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating alert suppression rule {RuleId}", id);
            throw;
        }
    }

    /// <summary>
    /// Deletes a suppression rule
    /// </summary>
    public async Task<bool> DeleteAsync(string id, CancellationToken cancellationToken = default)
    {
        try
        {
            var result = await _collection.DeleteOneAsync(r => r.Id == id, cancellationToken);
            
            if (result.DeletedCount > 0)
            {
                _logger.LogInformation("Deleted alert suppression rule {RuleId}", id);
                return true;
            }
            
            _logger.LogWarning("No alert suppression rule found with ID {RuleId} to delete", id);
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting alert suppression rule {RuleId}", id);
            throw;
        }
    }

    /// <summary>
    /// Increments the usage count for a rule
    /// </summary>
    public async Task IncrementRuleUsageAsync(string id, CancellationToken cancellationToken = default)
    {
        try
        {
            var update = Builders<AlertSuppressionRule>.Update.Inc(r => r.TimesTriggered, 1);
            
            await _collection.UpdateOneAsync(
                r => r.Id == id,
                update,
                null,
                cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error incrementing usage count for suppression rule {RuleId}", id);
            // Don't throw here as this is not critical
        }
    }

    /// <summary>
    /// Deletes expired suppression rules
    /// </summary>
    public async Task<int> DeleteExpiredRulesAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var now = DateTime.UtcNow;
            var filter = Builders<AlertSuppressionRule>.Filter.And(
                Builders<AlertSuppressionRule>.Filter.Ne(r => r.ExpiresAt, null),
                Builders<AlertSuppressionRule>.Filter.Lt(r => r.ExpiresAt, now)
            );

            var result = await _collection.DeleteManyAsync(filter, cancellationToken);
            
            _logger.LogInformation("Deleted {Count} expired alert suppression rules", result.DeletedCount);
            return (int)result.DeletedCount;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting expired alert suppression rules");
            throw;
        }
    }

    /// <summary>
    /// Creates necessary indexes for the collection
    /// </summary>
    private void CreateIndexes()
    {
        try
        {
            // Index for finding active rules
            var activeRulesIndex = Builders<AlertSuppressionRule>.IndexKeys
                .Ascending(r => r.IsActive)
                .Ascending(r => r.ExpiresAt);
            
            _collection.Indexes.CreateOne(new CreateIndexModel<AlertSuppressionRule>(activeRulesIndex));

            // Index for cleanup operations
            var expirationIndex = Builders<AlertSuppressionRule>.IndexKeys
                .Ascending(r => r.ExpiresAt);
            
            _collection.Indexes.CreateOne(new CreateIndexModel<AlertSuppressionRule>(expirationIndex));

            // Index for usage statistics
            var usageIndex = Builders<AlertSuppressionRule>.IndexKeys
                .Descending(r => r.TimesTriggered);
            
            _collection.Indexes.CreateOne(new CreateIndexModel<AlertSuppressionRule>(usageIndex));

            _logger.LogInformation("Created indexes for AlertSuppressionRule collection");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating indexes for AlertSuppressionRule collection");
        }
    }
}