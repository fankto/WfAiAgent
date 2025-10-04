namespace WorkflowPlus.AIAgent.MultiAgent;

/// <summary>
/// Represents an atomic subtask in a decomposed user request.
/// </summary>
public class SubTask
{
    public int Id { get; set; }
    public string Description { get; set; } = string.Empty;
    public List<int> DependsOn { get; set; } = new();
    public SubTaskStatus Status { get; set; } = SubTaskStatus.Pending;
}

/// <summary>
/// Status of a subtask during execution.
/// </summary>
public enum SubTaskStatus
{
    Pending,
    InProgress,
    Completed,
    Failed
}
