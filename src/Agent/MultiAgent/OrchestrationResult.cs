namespace WorkflowPlus.AIAgent.MultiAgent;

/// <summary>
/// Result of multi-agent orchestration.
/// </summary>
public class OrchestrationResult
{
    public string Script { get; set; } = string.Empty;
    public bool Success { get; set; }
    public OrchestrationMetrics Metrics { get; set; } = new();
    public List<string> Errors { get; set; } = new();
    public List<string> Warnings { get; set; } = new();
}

/// <summary>
/// Metrics collected during orchestration.
/// </summary>
public class OrchestrationMetrics
{
    public int SubTaskCount { get; set; }
    public int TotalCommandsFound { get; set; }
    public TimeSpan DecompositionTime { get; set; }
    public TimeSpan SearchTime { get; set; }
    public TimeSpan AssemblyTime { get; set; }
    public TimeSpan TotalTime { get; set; }
    public decimal EstimatedCost { get; set; }
}
