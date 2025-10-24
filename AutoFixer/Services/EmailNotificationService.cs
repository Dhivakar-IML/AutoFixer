using AutoFixer.Models;
using AutoFixer.Services.Interfaces;
using System.Net;
using System.Net.Mail;
using System.Text;

namespace AutoFixer.Services;

/// <summary>
/// Service for sending email notifications
/// </summary>
public class EmailNotificationService : IEmailNotificationService
{
    private readonly ILogger<EmailNotificationService> _logger;
    private readonly SmtpClient _smtpClient;

    public EmailNotificationService(ILogger<EmailNotificationService> logger)
    {
        _logger = logger;
        
        // Configure SMTP client - in production, these should come from configuration
        _smtpClient = new SmtpClient
        {
            Host = "smtp.company.com", // Replace with actual SMTP server
            Port = 587,
            EnableSsl = true,
            Credentials = new NetworkCredential("autofixer@company.com", "password") // Use secure credential management
        };
    }

    public async Task<bool> SendAlertAsync(PatternAlert alert, List<string> recipients, CancellationToken cancellationToken = default)
    {
        try
        {
            var subject = GenerateAlertSubject(alert);
            var body = GenerateAlertBody(alert);

            var mailMessage = new MailMessage
            {
                From = new MailAddress("autofixer@company.com", "AutoFixer"),
                Subject = subject,
                Body = body,
                IsBodyHtml = true
            };

            foreach (var recipient in recipients)
            {
                mailMessage.To.Add(recipient);
            }

            await _smtpClient.SendMailAsync(mailMessage, cancellationToken);

            _logger.LogDebug("Successfully sent email alert for {AlertId} to {RecipientCount} recipients", 
                alert.Id, recipients.Count);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending email alert for {AlertId}", alert.Id);
            return false;
        }
    }

    public async Task<bool> SendDigestAsync(List<PatternAlert> alerts, List<string> recipients, CancellationToken cancellationToken = default)
    {
        try
        {
            var subject = $"AutoFixer Daily Digest - {alerts.Count} Active Alerts";
            var body = GenerateDigestBody(alerts);

            var mailMessage = new MailMessage
            {
                From = new MailAddress("autofixer@company.com", "AutoFixer"),
                Subject = subject,
                Body = body,
                IsBodyHtml = true
            };

            foreach (var recipient in recipients)
            {
                mailMessage.To.Add(recipient);
            }

            await _smtpClient.SendMailAsync(mailMessage, cancellationToken);

            _logger.LogDebug("Successfully sent digest email with {AlertCount} alerts to {RecipientCount} recipients", 
                alerts.Count, recipients.Count);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending digest email");
            return false;
        }
    }

    private string GenerateAlertSubject(PatternAlert alert)
    {
        var severityPrefix = alert.Severity switch
        {
            AlertSeverity.Critical => "[CRITICAL]",
            AlertSeverity.Emergency => "[EMERGENCY]",
            AlertSeverity.Warning => "[WARNING]",
            AlertSeverity.Info => "[INFO]",
            _ => "[ALERT]"
        };

        var escalationSuffix = alert.EscalationLevel > 0 ? $" - ESCALATION LEVEL {alert.EscalationLevel}" : "";
        
        return $"{severityPrefix} AutoFixer Alert: {alert.PatternName}{escalationSuffix}";
    }

    private string GenerateAlertBody(PatternAlert alert)
    {
        var html = new StringBuilder();
        
        html.AppendLine("<!DOCTYPE html>");
        html.AppendLine("<html>");
        html.AppendLine("<head>");
        html.AppendLine("<style>");
        html.AppendLine("body { font-family: Arial, sans-serif; margin: 20px; }");
        html.AppendLine(".header { background-color: #f4f4f4; padding: 20px; border-radius: 5px; }");
        html.AppendLine(".critical { background-color: #ffebee; border-left: 5px solid #f44336; }");
        html.AppendLine(".high { background-color: #fff3e0; border-left: 5px solid #ff9800; }");
        html.AppendLine(".medium { background-color: #e3f2fd; border-left: 5px solid #2196f3; }");
        html.AppendLine(".low { background-color: #f1f8e9; border-left: 5px solid #4caf50; }");
        html.AppendLine(".details { margin: 20px 0; }");
        html.AppendLine(".fact { margin: 5px 0; }");
        html.AppendLine(".actions { margin: 20px 0; }");
        html.AppendLine(".button { display: inline-block; padding: 10px 15px; margin: 5px; text-decoration: none; border-radius: 3px; }");
        html.AppendLine(".primary { background-color: #2196f3; color: white; }");
        html.AppendLine(".secondary { background-color: #757575; color: white; }");
        html.AppendLine("</style>");
        html.AppendLine("</head>");
        html.AppendLine("<body>");

        var cssClass = alert.Severity.ToString().ToLower();
        var emoji = GetAlertEmoji(alert.Severity);

        html.AppendLine($"<div class=\"header {cssClass}\">");
        html.AppendLine($"<h2>{emoji} AutoFixer Alert: {alert.PatternName}</h2>");
        if (alert.EscalationLevel > 0)
        {
            html.AppendLine($"<p style=\"color: red; font-weight: bold;\">⚠️ ESCALATION LEVEL {alert.EscalationLevel} - REQUIRES IMMEDIATE ATTENTION</p>");
        }
        html.AppendLine($"<p>{alert.Message}</p>");
        html.AppendLine("</div>");

        html.AppendLine("<div class=\"details\">");
        html.AppendLine("<h3>Alert Details</h3>");
        html.AppendLine($"<div class=\"fact\"><strong>Severity:</strong> {alert.Severity}</div>");
        html.AppendLine($"<div class=\"fact\"><strong>Pattern ID:</strong> {alert.PatternId}</div>");
        html.AppendLine($"<div class=\"fact\"><strong>Affected Users:</strong> {alert.AffectedUsers}</div>");
        html.AppendLine($"<div class=\"fact\"><strong>Occurrence Rate:</strong> {alert.OccurrenceRate:F1} errors/hour</div>");
        html.AppendLine($"<div class=\"fact\"><strong>Trigger Count:</strong> {alert.TriggerCount}</div>");
        html.AppendLine($"<div class=\"fact\"><strong>Created:</strong> {alert.CreatedAt:yyyy-MM-dd HH:mm:ss UTC}</div>");

        if (alert.AffectedServices.Any())
        {
            html.AppendLine($"<div class=\"fact\"><strong>Affected Services:</strong> {string.Join(", ", alert.AffectedServices)}</div>");
        }

        html.AppendLine("</div>");

        html.AppendLine("<div class=\"actions\">");
        html.AppendLine("<h3>Actions</h3>");
        html.AppendLine($"<a href=\"https://autofixer.company.com/alerts/{alert.Id}/acknowledge\" class=\"button primary\">Acknowledge Alert</a>");
        html.AppendLine($"<a href=\"https://autofixer.company.com/patterns/{alert.PatternId}\" class=\"button secondary\">View Pattern Details</a>");
        html.AppendLine("</div>");

        html.AppendLine("<hr>");
        html.AppendLine("<p style=\"font-size: 12px; color: #666;\">This is an automated message from AutoFixer. Please do not reply to this email.</p>");
        
        html.AppendLine("</body>");
        html.AppendLine("</html>");

        return html.ToString();
    }

    private string GenerateDigestBody(List<PatternAlert> alerts)
    {
        var html = new StringBuilder();
        
        html.AppendLine("<!DOCTYPE html>");
        html.AppendLine("<html>");
        html.AppendLine("<head>");
        html.AppendLine("<style>");
        html.AppendLine("body { font-family: Arial, sans-serif; margin: 20px; }");
        html.AppendLine("table { border-collapse: collapse; width: 100%; }");
        html.AppendLine("th, td { border: 1px solid #ddd; padding: 8px; text-align: left; }");
        html.AppendLine("th { background-color: #f2f2f2; }");
        html.AppendLine(".critical { background-color: #ffebee; }");
        html.AppendLine(".high { background-color: #fff3e0; }");
        html.AppendLine(".medium { background-color: #e3f2fd; }");
        html.AppendLine(".low { background-color: #f1f8e9; }");
        html.AppendLine("</style>");
        html.AppendLine("</head>");
        html.AppendLine("<body>");

        html.AppendLine("<h2>AutoFixer Daily Digest</h2>");
        html.AppendLine($"<p>Summary of {alerts.Count} active alerts as of {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss UTC}</p>");

        // Summary statistics
        var severityCounts = alerts.GroupBy(a => a.Severity).ToDictionary(g => g.Key, g => g.Count());
        html.AppendLine("<h3>Summary</h3>");
        html.AppendLine("<ul>");
        foreach (var (severity, count) in severityCounts.OrderByDescending(kvp => kvp.Key))
        {
            html.AppendLine($"<li>{severity}: {count} alerts</li>");
        }
        html.AppendLine("</ul>");

        // Alert table
        html.AppendLine("<h3>Active Alerts</h3>");
        html.AppendLine("<table>");
        html.AppendLine("<tr>");
        html.AppendLine("<th>Pattern Name</th>");
        html.AppendLine("<th>Severity</th>");
        html.AppendLine("<th>Affected Users</th>");
        html.AppendLine("<th>Rate (/hour)</th>");
        html.AppendLine("<th>Created</th>");
        html.AppendLine("<th>Actions</th>");
        html.AppendLine("</tr>");

        foreach (var alert in alerts.OrderByDescending(a => a.Severity).ThenByDescending(a => a.CreatedAt))
        {
            var rowClass = alert.Severity.ToString().ToLower();
            html.AppendLine($"<tr class=\"{rowClass}\">");
            html.AppendLine($"<td>{alert.PatternName}</td>");
            html.AppendLine($"<td>{alert.Severity}</td>");
            html.AppendLine($"<td>{alert.AffectedUsers}</td>");
            html.AppendLine($"<td>{alert.OccurrenceRate:F1}</td>");
            html.AppendLine($"<td>{alert.CreatedAt:MM-dd HH:mm}</td>");
            html.AppendLine($"<td><a href=\"https://autofixer.company.com/alerts/{alert.Id}\">View</a></td>");
            html.AppendLine("</tr>");
        }

        html.AppendLine("</table>");

        html.AppendLine("<hr>");
        html.AppendLine("<p style=\"font-size: 12px; color: #666;\">This is an automated digest from AutoFixer.</p>");
        
        html.AppendLine("</body>");
        html.AppendLine("</html>");

        return html.ToString();
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

    protected virtual void Dispose(bool disposing)
    {
        if (disposing)
        {
            _smtpClient?.Dispose();
        }
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }
}