using AutoFixer.Models;
using AutoFixer.Services.Interfaces;
using System.Text.Json;
using System.Text;

namespace AutoFixer.Services;

/// <summary>
/// Service for sending Microsoft Teams notifications via webhooks
/// </summary>
public class TeamsNotificationService : ITeamsNotificationService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<TeamsNotificationService> _logger;

    public TeamsNotificationService(HttpClient httpClient, ILogger<TeamsNotificationService> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<bool> SendAlertAsync(PatternAlert alert, string webhookUrl, CancellationToken cancellationToken = default)
    {
        try
        {
            var teamsMessage = CreateAlertMessage(alert);
            var jsonContent = JsonSerializer.Serialize(teamsMessage, new JsonSerializerOptions 
            { 
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase 
            });

            var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");
            var response = await _httpClient.PostAsync(webhookUrl, content, cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                _logger.LogDebug("Successfully sent Teams alert for {AlertId}", alert.Id);
                return true;
            }
            else
            {
                _logger.LogWarning("Failed to send Teams alert for {AlertId}. Status: {StatusCode}", 
                    alert.Id, response.StatusCode);
                return false;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending Teams alert for {AlertId}", alert.Id);
            return false;
        }
    }

    public async Task<bool> SendEscalationAsync(PatternAlert alert, string webhookUrl, CancellationToken cancellationToken = default)
    {
        try
        {
            var teamsMessage = CreateEscalationMessage(alert);
            var jsonContent = JsonSerializer.Serialize(teamsMessage, new JsonSerializerOptions 
            { 
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase 
            });

            var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");
            var response = await _httpClient.PostAsync(webhookUrl, content, cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                _logger.LogDebug("Successfully sent Teams escalation for {AlertId}", alert.Id);
                return true;
            }
            else
            {
                _logger.LogWarning("Failed to send Teams escalation for {AlertId}. Status: {StatusCode}", 
                    alert.Id, response.StatusCode);
                return false;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending Teams escalation for {AlertId}", alert.Id);
            return false;
        }
    }

    private object CreateAlertMessage(PatternAlert alert)
    {
        var color = GetAlertColor(alert.Severity);
        var emoji = GetAlertEmoji(alert.Severity);

        return new
        {
            Type = "message",
            Attachments = new[]
            {
                new
                {
                    ContentType = "application/vnd.microsoft.card.adaptive",
                    Content = new
                    {
                        Type = "AdaptiveCard",
                        Schema = "http://adaptivecards.io/schemas/adaptive-card.json",
                        Version = "1.3",
                        Body = new object[]
                        {
                            new
                            {
                                Type = "TextBlock",
                                Text = $"{emoji} AutoFixer Alert",
                                Weight = "Bolder",
                                Size = "Medium"
                            },
                            new
                            {
                                Type = "TextBlock",
                                Text = alert.PatternName,
                                Weight = "Bolder",
                                Color = color
                            },
                            new
                            {
                                Type = "TextBlock",
                                Text = alert.Message,
                                Wrap = true
                            },
                            new
                            {
                                Type = "FactSet",
                                Facts = new[]
                                {
                                    new { Title = "Severity", Value = alert.Severity.ToString() },
                                    new { Title = "Affected Users", Value = alert.AffectedUsers.ToString() },
                                    new { Title = "Occurrence Rate", Value = $"{alert.OccurrenceRate:F1}/hour" },
                                    new { Title = "Pattern ID", Value = alert.PatternId },
                                    new { Title = "Created", Value = alert.CreatedAt.ToString("yyyy-MM-dd HH:mm:ss UTC") }
                                }
                            }
                        },
                        Actions = new[]
                        {
                            new
                            {
                                Type = "Action.OpenUrl",
                                Title = "Acknowledge Alert",
                                Url = $"https://autofixer.company.com/alerts/{alert.Id}/acknowledge"
                            },
                            new
                            {
                                Type = "Action.OpenUrl",
                                Title = "View Pattern Details",
                                Url = $"https://autofixer.company.com/patterns/{alert.PatternId}"
                            }
                        }
                    }
                }
            }
        };
    }

    private object CreateEscalationMessage(PatternAlert alert)
    {
        return new
        {
            Type = "message",
            Attachments = new[]
            {
                new
                {
                    ContentType = "application/vnd.microsoft.card.adaptive",
                    Content = new
                    {
                        Type = "AdaptiveCard",
                        Schema = "http://adaptivecards.io/schemas/adaptive-card.json",
                        Version = "1.3",
                        Body = new object[]
                        {
                            new
                            {
                                Type = "TextBlock",
                                Text = "🚨 ESCALATED ALERT",
                                Weight = "Bolder",
                                Size = "Large",
                                Color = "Attention"
                            },
                            new
                            {
                                Type = "TextBlock",
                                Text = alert.PatternName,
                                Weight = "Bolder",
                                Size = "Medium"
                            },
                            new
                            {
                                Type = "TextBlock",
                                Text = $"Alert has been escalated to level {alert.EscalationLevel}. Requires immediate attention!",
                                Wrap = true,
                                Color = "Attention"
                            },
                            new
                            {
                                Type = "FactSet",
                                Facts = new[]
                                {
                                    new { Title = "Escalation Level", Value = alert.EscalationLevel.ToString() },
                                    new { Title = "Original Severity", Value = alert.Severity.ToString() },
                                    new { Title = "Time Since Created", Value = GetTimeSince(alert.CreatedAt) },
                                    new { Title = "Trigger Count", Value = alert.TriggerCount.ToString() },
                                    new { Title = "Pattern ID", Value = alert.PatternId }
                                }
                            }
                        },
                        Actions = new[]
                        {
                            new
                            {
                                Type = "Action.OpenUrl",
                                Title = "Acknowledge Immediately",
                                Url = $"https://autofixer.company.com/alerts/{alert.Id}/acknowledge"
                            },
                            new
                            {
                                Type = "Action.OpenUrl",
                                Title = "View Full Details",
                                Url = $"https://autofixer.company.com/patterns/{alert.PatternId}"
                            }
                        }
                    }
                }
            }
        };
    }

    private string GetAlertColor(AlertSeverity severity)
    {
        return severity switch
        {
            AlertSeverity.Critical => "Attention",
            AlertSeverity.Emergency => "Attention",
            AlertSeverity.Warning => "Warning",
            AlertSeverity.Info => "Good",
            _ => "Default"
        };
    }

    private string GetAlertEmoji(AlertSeverity severity)
    {
        return severity switch
        {
            AlertSeverity.Critical => "🚨",
            AlertSeverity.Emergency => "🚨",
            AlertSeverity.Warning => "⚠️",
            AlertSeverity.Info => "ℹ️",
            _ => "📋"
        };
    }

    private string GetTimeSince(DateTime dateTime)
    {
        var timeSpan = DateTime.UtcNow - dateTime;
        
        if (timeSpan.TotalMinutes < 60)
            return $"{timeSpan.TotalMinutes:F0} minutes ago";
        if (timeSpan.TotalHours < 24)
            return $"{timeSpan.TotalHours:F1} hours ago";
        
        return $"{timeSpan.TotalDays:F1} days ago";
    }
}