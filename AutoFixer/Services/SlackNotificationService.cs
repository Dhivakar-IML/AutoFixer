using AutoFixer.Models;
using AutoFixer.Services.Interfaces;
using Slack.Webhooks;
using System.Text.Json;

namespace AutoFixer.Services;

/// <summary>
/// Service for sending Slack notifications
/// </summary>
public class SlackNotificationService : ISlackNotificationService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<SlackNotificationService> _logger;

    public SlackNotificationService(HttpClient httpClient, ILogger<SlackNotificationService> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<bool> SendAlertAsync(PatternAlert alert, string webhookUrl, CancellationToken cancellationToken = default)
    {
        try
        {
            var slackMessage = CreateAlertMessage(alert);
            var client = new SlackClient(webhookUrl);
            
            var result = await client.PostAsync(slackMessage);
            
            if (result)
            {
                _logger.LogDebug("Successfully sent Slack alert for {AlertId}", alert.Id);
            }
            else
            {
                _logger.LogWarning("Failed to send Slack alert for {AlertId}", alert.Id);
            }

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending Slack alert for {AlertId}", alert.Id);
            return false;
        }
    }

    public async Task<bool> SendEscalationAsync(PatternAlert alert, string webhookUrl, CancellationToken cancellationToken = default)
    {
        try
        {
            var slackMessage = CreateEscalationMessage(alert);
            var client = new SlackClient(webhookUrl);
            
            var result = await client.PostAsync(slackMessage);
            
            if (result)
            {
                _logger.LogDebug("Successfully sent Slack escalation for {AlertId}", alert.Id);
            }
            else
            {
                _logger.LogWarning("Failed to send Slack escalation for {AlertId}", alert.Id);
            }

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending Slack escalation for {AlertId}", alert.Id);
            return false;
        }
    }

    private SlackMessage CreateAlertMessage(PatternAlert alert)
    {
        var color = GetAlertColor(alert.Severity);
        var emoji = GetAlertEmoji(alert.Severity);
        
        var attachment = new SlackAttachment
        {
            Color = color,
            Title = $"{emoji} AutoFixer Alert: {alert.PatternName}",
            TitleLink = $"https://autofixer.company.com/patterns/{alert.PatternId}", // Replace with actual URL
            Text = alert.Message,
            Timestamp = (int)((DateTimeOffset)alert.CreatedAt).ToUnixTimeSeconds(),
            Fields = new List<SlackField>
            {
                new() { Title = "Severity", Value = alert.Severity.ToString(), Short = true },
                new() { Title = "Affected Users", Value = alert.AffectedUsers.ToString(), Short = true },
                new() { Title = "Occurrence Rate", Value = $"{alert.OccurrenceRate:F1}/hour", Short = true },
                new() { Title = "Pattern ID", Value = alert.PatternId, Short = true }
            }
        };

        if (alert.AffectedServices.Any())
        {
            attachment.Fields.Add(new SlackField
            {
                Title = "Affected Services",
                Value = string.Join(", ", alert.AffectedServices.Take(5)),
                Short = false
            });
        }

        // Add action buttons
        attachment.Actions = new List<SlackAction>
        {
            new()
            {
                Type = SlackActionType.Button,
                Text = "Acknowledge",
                Style = SlackActionStyle.Primary,
                Url = $"https://autofixer.company.com/alerts/{alert.Id}/acknowledge"
            },
            new()
            {
                Type = SlackActionType.Button,
                Text = "View Pattern",
                Url = $"https://autofixer.company.com/patterns/{alert.PatternId}"
            }
        };

        return new SlackMessage
        {
            Username = "AutoFixer",
            IconEmoji = ":robot_face:",
            Attachments = new List<SlackAttachment> { attachment }
        };
    }

    private SlackMessage CreateEscalationMessage(PatternAlert alert)
    {
        var attachment = new SlackAttachment
        {
            Color = "danger",
            Title = $"🚨 ESCALATION - AutoFixer Alert: {alert.PatternName}",
            TitleLink = $"https://autofixer.company.com/patterns/{alert.PatternId}",
            Text = $"Alert has been escalated to level {alert.EscalationLevel}. Requires immediate attention!",
            Timestamp = (int)((DateTimeOffset)(alert.LastEscalated ?? alert.CreatedAt)).ToUnixTimeSeconds(),
            Fields = new List<SlackField>
            {
                new() { Title = "Escalation Level", Value = alert.EscalationLevel.ToString(), Short = true },
                new() { Title = "Original Severity", Value = alert.Severity.ToString(), Short = true },
                new() { Title = "Time Since Created", Value = GetTimeSince(alert.CreatedAt), Short = true },
                new() { Title = "Trigger Count", Value = alert.TriggerCount.ToString(), Short = true }
            }
        };

        return new SlackMessage
        {
            Username = "AutoFixer Escalation",
            IconEmoji = ":warning:",
            Text = "<!channel> Escalated Alert Requires Attention",
            Attachments = new List<SlackAttachment> { attachment }
        };
    }

    private string GetAlertColor(AlertSeverity severity)
    {
        return severity switch
        {
            AlertSeverity.Critical => "danger",
            AlertSeverity.Emergency => "danger",
            AlertSeverity.Warning => "warning",
            AlertSeverity.Info => "good",
            _ => "good"
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