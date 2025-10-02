namespace WorkflowPlus.AIAgent.Core.Models;

/// <summary>
/// Represents the result of a tool invocation by the agent.
/// </summary>
public class ToolCallResult
{
    public string ToolName { get; set; } = string.Empty;
    public string Input { get; set; } = string.Empty;
    public string Output { get; set; } = string.Empty;
    public int DurationMs { get; set; }
    public bool Success { get; set; } = true;
    public string? ErrorMessage { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}
