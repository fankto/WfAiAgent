namespace WorkflowPlus.AIAgent.MultiAgent;

/// <summary>
/// Orchestrates the entire multi-agent workflow using unified pattern.
/// Handles all requests (simple and complex) with the same code path.
/// </summary>
public interface IMultiAgentOrchestrator
{
    /// <summary>
    /// Processes any user request using the unified multi-agent pattern.
    /// Always decomposes (N ≥ 1), spawns N agents, and assembles results.
    /// </summary>
    Task<OrchestrationResult> ProcessRequestAsync(string userRequest);
}
