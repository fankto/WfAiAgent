namespace WorkflowPlus.AIAgent.MultiAgent;

/// <summary>
/// Specialist agent that executes focused search for a single subtask.
/// </summary>
public interface ISpecialistSearchAgent
{
    /// <summary>
    /// Searches for commands relevant to a specific subtask.
    /// Returns top 1-3 commands for the subtask.
    /// </summary>
    Task<SearchResult> SearchForSubtaskAsync(SubTask subtask, int maxCommands = 3);
}
