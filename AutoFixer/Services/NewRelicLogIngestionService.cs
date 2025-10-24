using Microsoft.Extensions.Options;
using System.Text.Json;
using System.Text.Json.Serialization;
using AutoFixer.Configuration;
using AutoFixer.Models;
using AutoFixer.Services.Interfaces;

namespace AutoFixer.Services;

/// <summary>
/// Service for ingesting error logs from New Relic
/// </summary>
public class NewRelicLogIngestionService : INewRelicLogIngestionService
{
    private readonly HttpClient _httpClient;
    private readonly NewRelicSettings _settings;
    private readonly ILogger<NewRelicLogIngestionService> _logger;

    public NewRelicLogIngestionService(HttpClient httpClient, IOptions<NewRelicSettings> settings, ILogger<NewRelicLogIngestionService> logger)
    {
        _httpClient = httpClient;
        _settings = settings.Value;
        _logger = logger;

        _httpClient.BaseAddress = new Uri(_settings.BaseUrl);
        _httpClient.Timeout = TimeSpan.FromSeconds(_settings.TimeoutSeconds);
        _httpClient.DefaultRequestHeaders.Add("X-Query-Key", _settings.ApiKey);
    }

    public async Task<IEnumerable<ErrorEntry>> IngestErrorsAsync(DateTime since, CancellationToken cancellationToken = default)
    {
        try
        {
            var nrql = BuildNewRelicQuery(since);
            var requestUri = $"/v1/accounts/{_settings.AccountId}/query?nrql={Uri.EscapeDataString(nrql)}";

            _logger.LogInformation("Fetching errors from New Relic since {Since}", since);

            var response = await _httpClient.GetAsync(requestUri, cancellationToken);
            response.EnsureSuccessStatusCode();

            var content = await response.Content.ReadAsStringAsync(cancellationToken);
            var newRelicResponse = JsonSerializer.Deserialize<NewRelicResponse>(content, new JsonSerializerOptions 
            { 
                PropertyNameCaseInsensitive = true 
            });

            if (newRelicResponse?.Results == null)
            {
                _logger.LogWarning("No results returned from New Relic");
                return Enumerable.Empty<ErrorEntry>();
            }

            var errorEntries = newRelicResponse.Results.Select(ConvertNewRelicEventToErrorEntry).ToList();
            
            _logger.LogInformation("Ingested {Count} errors from New Relic", errorEntries.Count);
            return errorEntries;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error ingesting logs from New Relic");
            throw;
        }
    }

    public async Task<bool> TestConnectionAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var testQuery = "SELECT count(*) FROM Transaction LIMIT 1";
            var requestUri = $"/v1/accounts/{_settings.AccountId}/query?nrql={Uri.EscapeDataString(testQuery)}";
            
            var response = await _httpClient.GetAsync(requestUri, cancellationToken);
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to connect to New Relic");
            return false;
        }
    }

    private string BuildNewRelicQuery(DateTime since)
    {
        var sinceTimestamp = ((DateTimeOffset)since).ToUnixTimeMilliseconds();
        
        return $@"
            SELECT timestamp, message, error.class, http.statusCode, user.id, request.uri
            FROM TransactionError
            WHERE appName = '{_settings.ApplicationName}'
              AND timestamp >= {sinceTimestamp}
            LIMIT {_settings.MaxEventsPerRequest}";
    }

    private ErrorEntry ConvertNewRelicEventToErrorEntry(NewRelicResult result)
    {
        var errorEntry = new ErrorEntry
        {
            Timestamp = DateTimeOffset.FromUnixTimeMilliseconds(result.Timestamp).DateTime,
            Message = result.Message ?? "Unknown error from New Relic",
            ExceptionType = result.ErrorClass,
            Source = "NewRelic",
            StatusCode = result.StatusCode,
            UserId = result.UserId,
            Endpoint = result.RequestUri,
            Severity = MapStatusCodeToSeverity(result.StatusCode),
            Context = new Dictionary<string, object>
            {
                { "NewRelicAppName", _settings.ApplicationName },
                { "Timestamp", result.Timestamp }
            }
        };

        // Add additional context if available
        if (!string.IsNullOrEmpty(result.ErrorClass))
            errorEntry.Context["ErrorClass"] = result.ErrorClass;

        return errorEntry;
    }

    private ErrorSeverity MapStatusCodeToSeverity(int? statusCode)
    {
        if (!statusCode.HasValue)
            return ErrorSeverity.Warning;

        return statusCode.Value switch
        {
            >= 500 => ErrorSeverity.Critical,
            >= 400 => ErrorSeverity.Warning,
            _ => ErrorSeverity.Info
        };
    }
}

/// <summary>
/// Response model for New Relic NRQL queries
/// </summary>
internal class NewRelicResponse
{
    public List<NewRelicResult>? Results { get; set; }
}

/// <summary>
/// Model for a New Relic query result
/// </summary>
internal class NewRelicResult
{
    public long Timestamp { get; set; }
    public string? Message { get; set; }
    
    [JsonPropertyName("error.class")]
    public string? ErrorClass { get; set; }
    
    [JsonPropertyName("http.statusCode")]
    public int? StatusCode { get; set; }
    
    [JsonPropertyName("user.id")]
    public string? UserId { get; set; }
    
    [JsonPropertyName("request.uri")]
    public string? RequestUri { get; set; }
}