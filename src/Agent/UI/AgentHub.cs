using Microsoft.AspNetCore.SignalR;
using Serilog;

namespace WorkflowPlus.AIAgent.UI;

/// <summary>
/// SignalR Hub for real-time communication between C# backend and JavaScript frontend.
/// </summary>
public class AgentHub : Hub
{
    private readonly ILogger _logger;

    public AgentHub()
    {
        _logger = Log.ForContext<AgentHub>();
    }

    public override async Task OnConnectedAsync()
    {
        _logger.Information("Client connected: {ConnectionId}", Context.ConnectionId);
        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        if (exception != null)
        {
            _logger.Warning(exception, "Client disconnected with error: {ConnectionId}", Context.ConnectionId);
        }
        else
        {
            _logger.Information("Client disconnected: {ConnectionId}", Context.ConnectionId);
        }
        await base.OnDisconnectedAsync(exception);
    }

    /// <summary>
    /// Send a token to the connected client.
    /// </summary>
    public async Task SendToken(string token)
    {
        await Clients.Caller.SendAsync("ReceiveToken", token);
    }

    /// <summary>
    /// Signal that response is complete.
    /// </summary>
    public async Task SendComplete()
    {
        await Clients.Caller.SendAsync("ReceiveComplete");
    }

    /// <summary>
    /// Send an error message to the client.
    /// </summary>
    public async Task SendError(string errorMessage)
    {
        await Clients.Caller.SendAsync("ReceiveError", errorMessage);
    }
}
