using MongoDB.Driver;
using MongoDB.Bson;
using Microsoft.Extensions.Options;
using AutoFixer.Configuration;
using AutoFixer.Models;

namespace AutoFixer.Data.Repositories;

/// <summary>
/// Generic repository implementation for MongoDB operations
/// </summary>
/// <typeparam name="T">Entity type</typeparam>
public class Repository<T> : IRepository<T> where T : class
{
    protected readonly IMongoCollection<T> _collection;
    protected readonly IMongoDatabase _database;

    public Repository(IMongoDatabase database, string collectionName)
    {
        _database = database;
        _collection = database.GetCollection<T>(collectionName);
    }

    public virtual async Task<T?> GetByIdAsync(string id, CancellationToken cancellationToken = default)
    {
        var filter = Builders<T>.Filter.Eq("_id", ObjectId.Parse(id));
        return await _collection.Find(filter).FirstOrDefaultAsync(cancellationToken);
    }

    public virtual async Task<IEnumerable<T>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _collection.Find(Builders<T>.Filter.Empty).ToListAsync(cancellationToken);
    }

    public virtual async Task<IEnumerable<T>> FindAsync(FilterDefinition<T> filter, CancellationToken cancellationToken = default)
    {
        return await _collection.Find(filter).ToListAsync(cancellationToken);
    }

    public virtual async Task<T> CreateAsync(T entity, CancellationToken cancellationToken = default)
    {
        await _collection.InsertOneAsync(entity, cancellationToken: cancellationToken);
        return entity;
    }

    public virtual async Task<bool> UpdateAsync(string id, T entity, CancellationToken cancellationToken = default)
    {
        var filter = Builders<T>.Filter.Eq("_id", ObjectId.Parse(id));
        var result = await _collection.ReplaceOneAsync(filter, entity, cancellationToken: cancellationToken);
        return result.ModifiedCount > 0;
    }

    public virtual async Task<bool> DeleteAsync(string id, CancellationToken cancellationToken = default)
    {
        var filter = Builders<T>.Filter.Eq("_id", ObjectId.Parse(id));
        var result = await _collection.DeleteOneAsync(filter, cancellationToken);
        return result.DeletedCount > 0;
    }

    public virtual async Task<long> CountAsync(FilterDefinition<T> filter, CancellationToken cancellationToken = default)
    {
        return await _collection.CountDocumentsAsync(filter, cancellationToken: cancellationToken);
    }

    public virtual async Task<IEnumerable<T>> GetPagedAsync(FilterDefinition<T> filter, int page, int pageSize, CancellationToken cancellationToken = default)
    {
        var skip = (page - 1) * pageSize;
        return await _collection.Find(filter)
            .Skip(skip)
            .Limit(pageSize)
            .ToListAsync(cancellationToken);
    }
}