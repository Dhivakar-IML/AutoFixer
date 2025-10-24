using Microsoft.Extensions.Options;
using System.Text.Json;
using AutoFixer.Configuration;
using AutoFixer.Models;
using AutoFixer.Services.Interfaces;

namespace AutoFixer.Services;

/// <summary>
/// Service for ingesting error logs from Seq
/// </summary>
public class SeqLogIngestionService : ISeqLogIngestionService
{
    private readonly HttpClient _httpClient;
    private readonly SeqSettings _settings;
    private readonly ILogger<SeqLogIngestionService> _logger;

    public SeqLogIngestionService(HttpClient httpClient, IOptions<SeqSettings> settings, ILogger<SeqLogIngestionService> logger)
    {
        _httpClient = httpClient;
        _settings = settings.Value;
        _logger = logger;

        _httpClient.BaseAddress = new Uri(_settings.ApiUrl);
        _httpClient.Timeout = TimeSpan.FromSeconds(_settings.TimeoutSeconds);

        if (!string.IsNullOrEmpty(_settings.ApiKey))
        {
            _httpClient.DefaultRequestHeaders.Add("X-Seq-ApiKey", _settings.ApiKey);
        }
    }

    public async Task<IEnumerable<ErrorEntry>> IngestErrorsAsync(DateTime since, CancellationToken cancellationToken = default)
    {
        try
        {
            var query = BuildSeqQuery(since);
            var requestUri = $"/api/events/signal?filter={Uri.EscapeDataString(query)}&count={_settings.MaxEventsPerRequest}";

            _logger.LogInformation("Fetching errors from Seq since {Since}", since);

            var response = await _httpClient.GetAsync(requestUri, cancellationToken);
            response.EnsureSuccessStatusCode();

            var content = await response.Content.ReadAsStringAsync(cancellationToken);
            var seqResponse = JsonSerializer.Deserialize<SeqEventsResponse>(content, new JsonSerializerOptions 
            { 
                PropertyNameCaseInsensitive = true 
            });

            if (seqResponse?.Events == null)
            {
                _logger.LogWarning("No events returned from Seq");
                return Enumerable.Empty<ErrorEntry>();
            }

            var errorEntries = seqResponse.Events.Select(ConvertSeqEventToErrorEntry).ToList();
            
            _logger.LogInformation("Ingested {Count} errors from Seq", errorEntries.Count);
            return errorEntries;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error ingesting logs from Seq");
            throw;
        }
    }

    public async Task<bool> TestConnectionAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _httpClient.GetAsync("/api/health", cancellationToken);
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to connect to Seq");
            return false;
        }
    }

    private string BuildSeqQuery(DateTime since)
    {
        // Build a Seq query to get error events
        return $@"
            @Level = 'Error' 
            and @Timestamp >= DateTime('{since:yyyy-MM-ddTHH:mm:ss.fffZ}')
            and @Message is not null";
    }

    private ErrorEntry ConvertSeqEventToErrorEntry(SeqEvent seqEvent)
    {
        var errorEntry = new ErrorEntry
        {
            Timestamp = seqEvent.Timestamp,
            Message = seqEvent.RenderedMessage ?? seqEvent.MessageTemplate ?? "Unknown error",
            Source = "Seq",
            Context = seqEvent.Properties ?? new Dictionary<string, object>(),
            Severity = MapSeqLevelToSeverity(seqEvent.Level)
        };

        // Extract exception information if available
        if (seqEvent.Exception != null)
        {
            var exceptionString = seqEvent.Exception.ToString();
            errorEntry.StackTrace = exceptionString;
            errorEntry.ExceptionType = ExtractExceptionType(exceptionString);
        }

        // Extract common properties
        if (seqEvent.Properties != null)
        {
            if (seqEvent.Properties.TryGetValue("UserId", out var userId))
                errorEntry.UserId = userId?.ToString();

            if (seqEvent.Properties.TryGetValue("RequestPath", out var endpoint))
                errorEntry.Endpoint = endpoint?.ToString();

            if (seqEvent.Properties.TryGetValue("StatusCode", out var statusCode) && 
                int.TryParse(statusCode?.ToString(), out var code))
                errorEntry.StatusCode = code;
        }

        return errorEntry;
    }

    private ErrorSeverity MapSeqLevelToSeverity(string? level)
    {
        return level?.ToUpperInvariant() switch
        {
            "ERROR" => ErrorSeverity.Critical,
            "WARNING" => ErrorSeverity.Warning,
            "FATAL" => ErrorSeverity.Emergency,
            _ => ErrorSeverity.Info
        };
    }

    private string? ExtractExceptionType(string? stackTrace)
    {
        if (string.IsNullOrEmpty(stackTrace))
            return null;

        var lines = stackTrace.Split('\n');
        var firstLine = lines.FirstOrDefault()?.Trim();
        
        if (string.IsNullOrEmpty(firstLine))
            return null;

        // Extract exception type from the first line
        var colonIndex = firstLine.IndexOf(':');
        if (colonIndex > 0)
        {
            var exceptionType = firstLine.Substring(0, colonIndex).Trim();
            // Get just the class name without namespace
            var lastDotIndex = exceptionType.LastIndexOf('.');
            return lastDotIndex > 0 ? exceptionType.Substring(lastDotIndex + 1) : exceptionType;
        }

        return firstLine;
    }
}

/// <summary>
/// Response model for Seq events API
/// </summary>
internal class SeqEventsResponse
{
    public List<SeqEvent>? Events { get; set; }
}

/// <summary>
/// Model for a Seq event
/// </summary>
internal class SeqEvent
{
    public DateTime Timestamp { get; set; }
    public string? Level { get; set; }
    public string? MessageTemplate { get; set; }
    public string? RenderedMessage { get; set; }
    public Dictionary<string, object>? Properties { get; set; }
    public object? Exception { get; set; }
}