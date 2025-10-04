namespace WorkflowPlus.AIAgent.MultiAgent;

/// <summary>
/// Result of script assembly.
/// </summary>
public class AssemblyResult
{
    public string Script { get; set; } = string.Empty;
    public bool Success { get; set; }
    public string ErrorMessage { get; set; } = string.Empty;
    public List<string> Warnings { get; set; } = new();
}
