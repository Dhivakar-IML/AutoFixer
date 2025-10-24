using MongoDB.Driver;
using Microsoft.Extensions.Options;
using AutoFixer.Configuration;

namespace AutoFixer.Data;

/// <summary>
/// MongoDB database context for AutoFixer
/// </summary>
public interface IAutoFixerDbContext
{
    IMongoDatabase Database { get; }
    IMongoCollection<T> GetCollection<T>(string name);
}

/// <summary>
/// Implementation of MongoDB database context
/// </summary>
public class AutoFixerDbContext : IAutoFixerDbContext
{
    private readonly IMongoDatabase _database;
    private readonly MongoDbSettings _settings;

    public AutoFixerDbContext(IOptions<MongoDbSettings> settings)
    {
        _settings = settings.Value;
        
        var clientSettings = MongoClientSettings.FromConnectionString(_settings.ConnectionString);
        clientSettings.ConnectTimeout = TimeSpan.FromSeconds(_settings.ConnectionTimeoutSeconds);
        clientSettings.MaxConnectionPoolSize = _settings.MaxConnectionPoolSize;
        
        if (_settings.UseSsl)
        {
            clientSettings.UseTls = true;
        }

        var client = new MongoClient(clientSettings);
        _database = client.GetDatabase(_settings.DatabaseName);
    }

    public IMongoDatabase Database => _database;

    public IMongoCollection<T> GetCollection<T>(string name)
    {
        return _database.GetCollection<T>(name);
    }
}