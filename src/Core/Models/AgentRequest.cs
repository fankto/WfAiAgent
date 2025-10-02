namespace WorkflowPlus.AIAgent.Core.Models;

/// <summary>
/// Represents a user request to the AI agent.
/// </summary>
public class AgentRequest
{
    public string Query { get; set; } = string.Empty;
    public string? ConversationId { get; set; }
    public string UserId { get; set; } = string.Empty;
    public Dictionary<string, object>? Context { get; set; }
}
