namespace WorkflowPlus.AIAgent.MultiAgent;

/// <summary>
/// Configuration settings for multi-agent orchestration.
/// </summary>
public class MultiAgentSettings
{
    public int MaxConcurrentAgents { get; set; } = 10;
    public int MaxSubTasksPerRequest { get; set; } = 20;
    public int AgentTimeoutSeconds { get; set; } = 30;
    
    public ModelSettings Models { get; set; } = new();
    public TimeoutSettings Timeouts { get; set; } = new();
    public LoggingSettings Logging { get; set; } = new();
}

public class ModelSettings
{
    public string DecompositionModel { get; set; } = "gpt-4o-mini";
    public string AssemblyModel { get; set; } = "gpt-4o-mini";
    public string SpecialistModel { get; set; } = "gpt-4o";
}

public class TimeoutSettings
{
    public int TaskDecompositionSeconds { get; set; } = 10;
    public int SpecialistSearchSeconds { get; set; } = 15;
    public int ScriptAssemblySeconds { get; set; } = 10;
}

public class LoggingSettings
{
    public bool LogDecomposition { get; set; } = true;
    public bool LogAgentExecution { get; set; } = true;
    public bool LogAssembly { get; set; } = true;
    public bool VerboseMetrics { get; set; } = true;
}
