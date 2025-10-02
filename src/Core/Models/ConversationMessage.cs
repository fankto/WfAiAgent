namespace WorkflowPlus.AIAgent.Core.Models;

/// <summary>
/// Represents a single message in a conversation.
/// </summary>
public class ConversationMessage
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string ConversationId { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty; // "user", "assistant", "tool"
    public string Content { get; set; } = string.Empty;
    public int? ReasoningTokens { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public Dictionary<string, object>? Metadata { get; set; }
}
