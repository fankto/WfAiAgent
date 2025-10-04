namespace WorkflowPlus.AIAgent.MultiAgent;

/// <summary>
/// Result of task decomposition.
/// </summary>
public class DecompositionResult
{
    public List<SubTask> SubTasks { get; set; } = new();
    public Dictionary<int, List<int>> Dependencies { get; set; } = new();
    public bool Success { get; set; }
    public string ErrorMessage { get; set; } = string.Empty;
}
