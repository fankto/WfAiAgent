using WorkflowPlus.AIAgent.Tools;

namespace WorkflowPlus.AIAgent.MultiAgent;

/// <summary>
/// Result from a specialist search agent.
/// </summary>
public class SearchResult
{
    public int SubTaskId { get; set; }
    public List<CommandMatch> Commands { get; set; } = new();
    public bool Success { get; set; }
    public string ErrorMessage { get; set; } = string.Empty;
    public TimeSpan ExecutionTime { get; set; }
}
