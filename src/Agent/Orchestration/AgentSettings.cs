using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace WorkflowPlus.AIAgent.Orchestration;

/// <summary>
/// Configuration settings for the AI agent.
/// </summary>
public class AgentSettings
{
    public string DefaultModel { get; set; } = "gpt-4o";
    public string FastModel { get; set; } = "gpt-4o-mini";
    public double Temperature { get; set; } = 0.7;
    public int MaxTokens { get; set; } = 4000;
    public decimal MaxCostPerQuery { get; set; } = 0.50m;
    public string SystemPrompt { get; set; } = string.Empty;
    public string CodeGenerationPrompt { get; set; } = string.Empty;
    public string ReflectionPrompt { get; set; } = string.Empty;
    public int MaxReflectionIterations { get; set; } = 3;

    public static AgentSettings LoadFromYaml(string yamlPath)
    {
        var yaml = File.ReadAllText(yamlPath);
        var deserializer = new DeserializerBuilder()
            .WithNamingConvention(PascalCaseNamingConvention.Instance)
            .Build();
        
        var config = deserializer.Deserialize<Dictionary<string, object>>(yaml);
        
        var settings = new AgentSettings();
        
        if (config.ContainsKey("SystemPrompts"))
        {
            var prompts = (Dictionary<object, object>)config["SystemPrompts"];
            settings.SystemPrompt = prompts["Orchestrator"]?.ToString() ?? string.Empty;
            settings.CodeGenerationPrompt = prompts["CodeGeneration"]?.ToString() ?? string.Empty;
            settings.ReflectionPrompt = prompts["Reflection"]?.ToString() ?? string.Empty;
        }
        
        if (config.ContainsKey("Models"))
        {
            var models = (Dictionary<object, object>)config["Models"];
            settings.DefaultModel = models["Primary"]?.ToString() ?? "gpt-4o";
            settings.FastModel = models["Fast"]?.ToString() ?? "gpt-4o-mini";
        }
        
        if (config.ContainsKey("Safety"))
        {
            var safety = (Dictionary<object, object>)config["Safety"];
            if (safety.ContainsKey("MaxCostPerQuery"))
                settings.MaxCostPerQuery = Convert.ToDecimal(safety["MaxCostPerQuery"]);
            if (safety.ContainsKey("MaxReflectionIterations"))
                settings.MaxReflectionIterations = Convert.ToInt32(safety["MaxReflectionIterations"]);
        }
        
        return settings;
    }
}
