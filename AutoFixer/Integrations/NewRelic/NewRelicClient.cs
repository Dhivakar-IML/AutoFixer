using System.Text.Json;
using AutoFixer.Models;

namespace AutoFixer.Integrations.NewRelic
{
    public class NewRelicClient
    {
        private readonly HttpClient _httpClient;
        private readonly string _apiKey;
        private readonly string _accountId;
        private readonly ILogger<NewRelicClient> _logger;

        public NewRelicClient(HttpClient httpClient, IConfiguration configuration, ILogger<NewRelicClient> logger)
        {
            _httpClient = httpClient;
            _apiKey = configuration["NewRelic:ApiKey"] ?? throw new ArgumentNullException("NewRelic:ApiKey");
            _accountId = configuration["NewRelic:AccountId"] ?? throw new ArgumentNullException("NewRelic:AccountId");
            _logger = logger;

            _httpClient.DefaultRequestHeaders.Add("Api-Key", _apiKey);
            _httpClient.BaseAddress = new Uri("https://api.newrelic.com/graphql");
        }

        public async Task<List<NewRelicError>> GetErrorsAsync(DateTime since, DateTime until, int limit = 100)
        {
            var query = @"
            {
                actor {
                    account(id: " + _accountId + @") {
                        nrql(query: ""SELECT * FROM JavaScriptError, TransactionError SINCE '" + since.ToString("yyyy-MM-dd HH:mm:ss") + @"' UNTIL '" + until.ToString("yyyy-MM-dd HH:mm:ss") + @"' LIMIT " + limit + @""") {
                            results
                        }
                    }
                }
            }";

            var request = new
            {
                query = query
            };

            var response = await _httpClient.PostAsJsonAsync("", request);
            var content = await response.Content.ReadAsStringAsync();
            
            _logger.LogInformation("New Relic API Response: {Response}", content);

            var newRelicResponse = JsonSerializer.Deserialize<NewRelicGraphQLResponse>(content);
            
            return newRelicResponse?.Data?.Actor?.Account?.Nrql?.Results
                ?.Select(MapToNewRelicError)
                ?.ToList() ?? new List<NewRelicError>();
        }

        public async Task<List<NewRelicIncident>> GetIncidentsAsync(DateTime since)
        {
            var query = @"
            {
                actor {
                    account(id: " + _accountId + @") {
                        aiIssues {
                            issues(filter: {states: [ACTIVATED, ACKNOWLEDGED]}) {
                                issues {
                                    issueId
                                    title
                                    state
                                    priority
                                    sources
                                    createdAt
                                    updatedAt
                                    description
                                    origins
                                }
                            }
                        }
                    }
                }
            }";

            var request = new { query = query };
            var response = await _httpClient.PostAsJsonAsync("", request);
            var content = await response.Content.ReadAsStringAsync();

            var newRelicResponse = JsonSerializer.Deserialize<NewRelicIncidentResponse>(content);
            
            return newRelicResponse?.Data?.Actor?.Account?.AiIssues?.Issues?.Issues
                ?.Where(i => DateTime.Parse(i.CreatedAt) >= since)
                ?.Select(MapToNewRelicIncident)
                ?.ToList() ?? new List<NewRelicIncident>();
        }

        public async Task<List<ErrorPattern>> ConvertToErrorPatternsAsync(List<NewRelicError> errors)
        {
            var patterns = new List<ErrorPattern>();
            var errorGroups = errors.GroupBy(e => new { e.ErrorMessage, e.ErrorClass, e.AppName });

            foreach (var group in errorGroups)
            {
                var errorList = group.ToList();
                var first = errorList.First();
                var last = errorList.Last();

                var pattern = new ErrorPattern
                {
                    Id = Guid.NewGuid().ToString(),
                    Name = $"{first.ErrorClass}: {TruncateMessage(first.ErrorMessage, 50)}",
                    Description = first.ErrorMessage,
                    ErrorType = DetermineErrorType(first.ErrorClass),
                    Priority = DeterminePriority(errorList.Count, first.ErrorClass),
                    Status = PatternStatus.Active,
                    Confidence = CalculateConfidence(errorList.Count, group.Key.ErrorMessage),
                    FirstOccurrence = DateTime.Parse(first.Timestamp),
                    LastOccurrence = DateTime.Parse(last.Timestamp),
                    OccurrenceCount = errorList.Count,
                    OccurrenceRate = CalculateOccurrenceRate(errorList),
                    AffectedServices = new List<string> { first.AppName },
                    UserImpact = EstimateUserImpact(errorList.Count),
                    TechnicalDetails = new TechnicalDetails
                    {
                        StackTrace = first.StackTrace,
                        ErrorCode = first.ErrorClass,
                        ComponentsAffected = new List<string> { first.AppName },
                        InfrastructureImpact = new InfrastructureImpact
                        {
                            CpuUsage = 0,
                            MemoryUsage = 0,
                            DiskUsage = 0,
                            NetworkLatency = 0
                        }
                    },
                    BusinessImpact = new BusinessImpact
                    {
                        RevenueImpact = EstimateRevenueImpact(errorList.Count, first.ErrorClass),
                        CustomerSatisfactionScore = EstimateCSAT(errorList.Count),
                        ServiceLevelImpact = EstimateSLImpact(errorList.Count)
                    },
                    Tags = new List<string> { "new-relic", first.AppName, first.ErrorClass }
                };

                patterns.Add(pattern);
            }

            return patterns;
        }

        private NewRelicError MapToNewRelicError(JsonElement result)
        {
            return new NewRelicError
            {
                Timestamp = GetJsonProperty(result, "timestamp"),
                ErrorMessage = GetJsonProperty(result, "error.message") ?? GetJsonProperty(result, "errorMessage"),
                ErrorClass = GetJsonProperty(result, "error.class") ?? GetJsonProperty(result, "errorClass"),
                StackTrace = GetJsonProperty(result, "stackTrace"),
                AppName = GetJsonProperty(result, "appName"),
                TransactionName = GetJsonProperty(result, "name"),
                Host = GetJsonProperty(result, "host"),
                UserAgent = GetJsonProperty(result, "userAgentName"),
                RequestUri = GetJsonProperty(result, "request.uri")
            };
        }

        private NewRelicIncident MapToNewRelicIncident(dynamic issue)
        {
            return new NewRelicIncident
            {
                IssueId = issue.issueId,
                Title = issue.title,
                State = issue.state,
                Priority = issue.priority,
                CreatedAt = issue.createdAt,
                UpdatedAt = issue.updatedAt,
                Description = issue.description,
                Sources = issue.sources?.ToObject<List<string>>() ?? new List<string>()
            };
        }

        private string GetJsonProperty(JsonElement element, string propertyName)
        {
            if (element.TryGetProperty(propertyName, out var property))
            {
                return property.GetString();
            }
            return string.Empty;
        }

        private ErrorType DetermineErrorType(string errorClass)
        {
            return errorClass?.ToLower() switch
            {
                var c when c.Contains("timeout") => ErrorType.Performance,
                var c when c.Contains("connection") => ErrorType.Infrastructure,
                var c when c.Contains("authentication") => ErrorType.Security,
                var c when c.Contains("authorization") => ErrorType.Security,
                var c when c.Contains("validation") => ErrorType.BusinessLogic,
                var c when c.Contains("null") => ErrorType.ApplicationLogic,
                var c when c.Contains("format") => ErrorType.DataQuality,
                _ => ErrorType.Unknown
            };
        }

        private PatternPriority DeterminePriority(int occurrenceCount, string errorClass)
        {
            if (occurrenceCount > 50 || errorClass.Contains("Critical"))
                return PatternPriority.Critical;
            if (occurrenceCount > 20 || errorClass.Contains("Error"))
                return PatternPriority.High;
            if (occurrenceCount > 5)
                return PatternPriority.Medium;
            return PatternPriority.Low;
        }

        private double CalculateConfidence(int occurrenceCount, string errorMessage)
        {
            var baseConfidence = Math.Min(0.9, occurrenceCount / 100.0 + 0.5);
            var messageSpecificity = Math.Min(0.3, errorMessage.Length / 200.0);
            return Math.Min(0.98, baseConfidence + messageSpecificity);
        }

        private double CalculateOccurrenceRate(List<NewRelicError> errors)
        {
            if (errors.Count < 2) return errors.Count;
            
            var timeSpan = DateTime.Parse(errors.Last().Timestamp) - DateTime.Parse(errors.First().Timestamp);
            return timeSpan.TotalHours > 0 ? errors.Count / timeSpan.TotalHours : errors.Count;
        }

        private int EstimateUserImpact(int errorCount)
        {
            // Rough estimation: each error affects 1-3 users on average
            return (int)(errorCount * 1.5);
        }

        private double EstimateRevenueImpact(int errorCount, string errorClass)
        {
            var baseImpact = errorClass?.ToLower() switch
            {
                var c when c.Contains("payment") => 100.0,
                var c when c.Contains("checkout") => 75.0,
                var c when c.Contains("login") => 25.0,
                var c when c.Contains("api") => 50.0,
                _ => 10.0
            };
            
            return errorCount * baseImpact;
        }

        private double EstimateCSAT(int errorCount)
        {
            // Customer satisfaction decreases with more errors
            return Math.Max(1.0, 5.0 - (errorCount / 20.0));
        }

        private double EstimateSLImpact(int errorCount)
        {
            // Service level impact as percentage
            return Math.Min(100.0, errorCount * 0.5);
        }

        private string TruncateMessage(string message, int maxLength)
        {
            if (string.IsNullOrEmpty(message)) return "Unknown Error";
            return message.Length > maxLength ? message.Substring(0, maxLength) + "..." : message;
        }
    }

    // Data Transfer Objects
    public class NewRelicError
    {
        public string Timestamp { get; set; } = string.Empty;
        public string ErrorMessage { get; set; } = string.Empty;
        public string ErrorClass { get; set; } = string.Empty;
        public string StackTrace { get; set; } = string.Empty;
        public string AppName { get; set; } = string.Empty;
        public string TransactionName { get; set; } = string.Empty;
        public string Host { get; set; } = string.Empty;
        public string UserAgent { get; set; } = string.Empty;
        public string RequestUri { get; set; } = string.Empty;
    }

    public class NewRelicIncident
    {
        public string IssueId { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string State { get; set; } = string.Empty;
        public string Priority { get; set; } = string.Empty;
        public string CreatedAt { get; set; } = string.Empty;
        public string UpdatedAt { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public List<string> Sources { get; set; } = new();
    }

    // GraphQL Response Models
    public class NewRelicGraphQLResponse
    {
        public NewRelicData Data { get; set; } = new();
    }

    public class NewRelicData
    {
        public NewRelicActor Actor { get; set; } = new();
    }

    public class NewRelicActor
    {
        public NewRelicAccount Account { get; set; } = new();
    }

    public class NewRelicAccount
    {
        public NewRelicNrql Nrql { get; set; } = new();
        public NewRelicAiIssues AiIssues { get; set; } = new();
    }

    public class NewRelicNrql
    {
        public List<JsonElement> Results { get; set; } = new();
    }

    public class NewRelicIncidentResponse
    {
        public NewRelicData Data { get; set; } = new();
    }

    public class NewRelicAiIssues
    {
        public NewRelicIssuesContainer Issues { get; set; } = new();
    }

    public class NewRelicIssuesContainer
    {
        public List<dynamic> Issues { get; set; } = new();
    }
}