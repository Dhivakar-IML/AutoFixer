namespace AutoFixer.Configuration;

/// <summary>
/// Configuration settings for MongoDB connection
/// </summary>
public class MongoDbSettings
{
    public const string SectionName = "MongoDB";

    /// <summary>
    /// MongoDB connection string
    /// </summary>
    public string ConnectionString { get; set; } = "mongodb://localhost:27017";

    /// <summary>
    /// Database name for AutoFixer
    /// </summary>
    public string DatabaseName { get; set; } = "AutoFixer";

    /// <summary>
    /// Collection names
    /// </summary>
    public string ErrorEntriesCollection { get; set; } = "ErrorEntries";
    public string ErrorClustersCollection { get; set; } = "ErrorClusters";
    public string ErrorPatternsCollection { get; set; } = "ErrorPatterns";
    public string RootCauseAnalysesCollection { get; set; } = "RootCauseAnalyses";
    public string PatternResolutionsCollection { get; set; } = "PatternResolutions";
    public string PatternAlertsCollection { get; set; } = "PatternAlerts";

    /// <summary>
    /// Connection timeout in seconds
    /// </summary>
    public int ConnectionTimeoutSeconds { get; set; } = 30;

    /// <summary>
    /// Socket timeout in seconds
    /// </summary>
    public int SocketTimeoutSeconds { get; set; } = 30;

    /// <summary>
    /// Maximum connection pool size
    /// </summary>
    public int MaxConnectionPoolSize { get; set; } = 100;

    /// <summary>
    /// Server selection timeout in seconds
    /// </summary>
    public int ServerSelectionTimeoutSeconds { get; set; } = 30;

    /// <summary>
    /// Whether to use SSL for connection
    /// </summary>
    public bool UseSsl { get; set; } = false;
}