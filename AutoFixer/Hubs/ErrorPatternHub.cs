using Microsoft.AspNetCore.SignalR;
using AutoFixer.Models;

namespace AutoFixer.Hubs;

/// <summary>
/// SignalR Hub for real-time error pattern notifications
/// </summary>
public class ErrorPatternHub : Hub
{
    private readonly ILogger<ErrorPatternHub> _logger;

    public ErrorPatternHub(ILogger<ErrorPatternHub> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Called when a client connects to the hub
    /// </summary>
    public override async Task OnConnectedAsync()
    {
        _logger.LogInformation("Client connected: {ConnectionId}", Context.ConnectionId);
        await base.OnConnectedAsync();
    }

    /// <summary>
    /// Called when a client disconnects from the hub
    /// </summary>
    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        _logger.LogInformation("Client disconnected: {ConnectionId}", Context.ConnectionId);
        await base.OnDisconnectedAsync(exception);
    }

    /// <summary>
    /// Send a new error pattern detection to all connected clients
    /// </summary>
    public async Task SendErrorPattern(ErrorPattern pattern)
    {
        _logger.LogInformation("Broadcasting error pattern: {PatternName}", pattern.Name);
        await Clients.All.SendAsync("ReceiveErrorPattern", pattern);
    }

    /// <summary>
    /// Send a new error entry to all connected clients
    /// </summary>
    public async Task SendErrorEntry(ErrorEntry entry)
    {
        _logger.LogInformation("Broadcasting error entry: {Source}", entry.Source);
        await Clients.All.SendAsync("ReceiveErrorEntry", entry);
    }

    /// <summary>
    /// Send pattern alert to all connected clients
    /// </summary>
    public async Task SendPatternAlert(PatternAlert alert)
    {
        _logger.LogInformation("Broadcasting pattern alert: {AlertId}", alert.Id);
        await Clients.All.SendAsync("ReceivePatternAlert", alert);
    }

    /// <summary>
    /// Send real-time statistics to all connected clients
    /// </summary>
    public async Task SendStatistics(object statistics)
    {
        await Clients.All.SendAsync("ReceiveStatistics", statistics);
    }

    /// <summary>
    /// Client subscribes to specific pattern updates
    /// </summary>
    public async Task SubscribeToPattern(string patternId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, $"pattern_{patternId}");
        _logger.LogInformation("Client {ConnectionId} subscribed to pattern {PatternId}", Context.ConnectionId, patternId);
    }

    /// <summary>
    /// Client unsubscribes from specific pattern updates
    /// </summary>
    public async Task UnsubscribeFromPattern(string patternId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"pattern_{patternId}");
        _logger.LogInformation("Client {ConnectionId} unsubscribed from pattern {PatternId}", Context.ConnectionId, patternId);
    }

    /// <summary>
    /// Send pattern update to subscribed clients only
    /// </summary>
    public async Task SendPatternUpdate(string patternId, object update)
    {
        await Clients.Group($"pattern_{patternId}").SendAsync("ReceivePatternUpdate", update);
    }
}
