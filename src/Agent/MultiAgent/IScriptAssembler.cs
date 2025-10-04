using WorkflowPlus.AIAgent.Tools;

namespace WorkflowPlus.AIAgent.MultiAgent;

/// <summary>
/// Assembles commands from multiple subtasks into a coherent script.
/// </summary>
public interface IScriptAssembler
{
    /// <summary>
    /// Assembles a script from commands found for each subtask.
    /// Handles both simple (N=1) and complex (N>1) cases.
    /// </summary>
    Task<AssemblyResult> AssembleScriptAsync(
        string userRequest,
        List<SubTask> subtasks,
        Dictionary<int, List<CommandMatch>> commandsBySubtask);
}
