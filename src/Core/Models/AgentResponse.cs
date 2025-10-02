namespace WorkflowPlus.AIAgent.Core.Models;

/// <summary>
/// Represents the agent's response to a user query.
/// </summary>
public class AgentResponse
{
    public string Content { get; set; } = string.Empty;
    public string? ConversationId { get; set; }
    public int TokensUsed { get; set; }
    public decimal EstimatedCost { get; set; }
    public string ModelUsed { get; set; } = string.Empty;
    public List<ToolCallResult>? ToolCalls { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public bool Success { get; set; } = true;
    public string? ErrorMessage { get; set; }
}
