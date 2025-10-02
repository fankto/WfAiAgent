using WorkflowPlus.AIAgent.Core.Models;

namespace WorkflowPlus.AIAgent.Core.Interfaces;

/// <summary>
/// Base interface for all agent tools.
/// </summary>
public interface IAgentTool
{
    string Name { get; }
    string Description { get; }
    Task<ToolCallResult> ExecuteAsync(Dictionary<string, object> parameters);
}
