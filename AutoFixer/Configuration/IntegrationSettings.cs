namespace AutoFixer.Configuration;

/// <summary>
/// Configuration settings for Seq integration
/// </summary>
public class SeqSettings
{
    public const string SectionName = "Seq";

    /// <summary>
    /// Seq API URL
    /// </summary>
    public string ApiUrl { get; set; } = "http://localhost:5341";

    /// <summary>
    /// API key for authentication
    /// </summary>
    public string? ApiKey { get; set; }

    /// <summary>
    /// Timeout for API requests in seconds
    /// </summary>
    public int TimeoutSeconds { get; set; } = 30;

    /// <summary>
    /// Maximum number of events to fetch in one request
    /// </summary>
    public int MaxEventsPerRequest { get; set; } = 10000;

    /// <summary>
    /// How far back to look for errors on startup (in hours)
    /// </summary>
    public int InitialLookbackHours { get; set; } = 24;
}

/// <summary>
/// Configuration settings for New Relic integration
/// </summary>
public class NewRelicSettings
{
    public const string SectionName = "NewRelic";

    /// <summary>
    /// New Relic account ID
    /// </summary>
    public string AccountId { get; set; } = string.Empty;

    /// <summary>
    /// API key for authentication
    /// </summary>
    public string ApiKey { get; set; } = string.Empty;

    /// <summary>
    /// Base URL for New Relic API
    /// </summary>
    public string BaseUrl { get; set; } = "https://insights-api.newrelic.com";

    /// <summary>
    /// Application name to filter events
    /// </summary>
    public string ApplicationName { get; set; } = "Registration-API";

    /// <summary>
    /// Timeout for API requests in seconds
    /// </summary>
    public int TimeoutSeconds { get; set; } = 30;

    /// <summary>
    /// Maximum number of events to fetch in one request
    /// </summary>
    public int MaxEventsPerRequest { get; set; } = 10000;
}

/// <summary>
/// Configuration settings for Slack integration
/// </summary>
public class SlackSettings
{
    public const string SectionName = "Slack";

    /// <summary>
    /// Webhook URL for sending alerts
    /// </summary>
    public string? WebhookUrl { get; set; }

    /// <summary>
    /// Default channel for alerts
    /// </summary>
    public string DefaultChannel { get; set; } = "#alerts";

    /// <summary>
    /// Bot username
    /// </summary>
    public string BotUsername { get; set; } = "AutoFixer";

    /// <summary>
    /// Whether to enable Slack alerts
    /// </summary>
    public bool Enabled { get; set; } = true;
}

/// <summary>
/// Configuration settings for Teams integration
/// </summary>
public class TeamsSettings
{
    public const string SectionName = "Teams";

    /// <summary>
    /// Webhook URL for sending alerts
    /// </summary>
    public string? WebhookUrl { get; set; }

    /// <summary>
    /// Whether to enable Teams alerts
    /// </summary>
    public bool Enabled { get; set; } = false;
}

/// <summary>
/// Configuration settings for ML processing
/// </summary>
public class MLSettings
{
    public const string SectionName = "ML";

    /// <summary>
    /// Similarity threshold for clustering (0.0 to 1.0)
    /// </summary>
    public double SimilarityThreshold { get; set; } = 0.85;

    /// <summary>
    /// Minimum number of errors required to form a cluster
    /// </summary>
    public int MinClusterSize { get; set; } = 5;

    /// <summary>
    /// Maximum number of features to extract for TF-IDF
    /// </summary>
    public int MaxFeatures { get; set; } = 10000;

    /// <summary>
    /// Whether to retrain model periodically
    /// </summary>
    public bool EnablePeriodicRetraining { get; set; } = true;

    /// <summary>
    /// How often to retrain the model (in hours)
    /// </summary>
    public int RetrainingIntervalHours { get; set; } = 24;
}