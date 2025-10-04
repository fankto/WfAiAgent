namespace WorkflowPlus.AIAgent.MultiAgent;

/// <summary>
/// Decomposes user requests into atomic subtasks.
/// </summary>
public interface ITaskDecomposer
{
    /// <summary>
    /// Decomposes a user request into N subtasks (N ≥ 1).
    /// For simple requests, returns a single subtask.
    /// For complex requests, returns multiple subtasks with dependencies.
    /// </summary>
    Task<DecompositionResult> DecomposeAsync(string userRequest);
}
