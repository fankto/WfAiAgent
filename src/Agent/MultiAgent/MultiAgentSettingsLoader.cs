using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace WorkflowPlus.AIAgent.MultiAgent;

/// <summary>
/// Loads multi-agent orchestration settings from YAML configuration.
/// </summary>
public static class MultiAgentSettingsLoader
{
    public static MultiAgentSettings LoadFromYaml(string yamlPath)
    {
        if (!File.Exists(yamlPath))
        {
            // Return defaults if config doesn't exist
            return new MultiAgentSettings();
        }

        var yaml = File.ReadAllText(yamlPath);
        var deserializer = new DeserializerBuilder()
            .WithNamingConvention(PascalCaseNamingConvention.Instance)
            .Build();
        
        var config = deserializer.Deserialize<Dictionary<string, object>>(yaml);
        
        var settings = new MultiAgentSettings();
        
        if (config.ContainsKey("MultiAgentOrchestration"))
        {
            var multiAgent = (Dictionary<object, object>)config["MultiAgentOrchestration"];
            
            if (multiAgent.ContainsKey("MaxConcurrentAgents"))
                settings.MaxConcurrentAgents = Convert.ToInt32(multiAgent["MaxConcurrentAgents"]);
            
            if (multiAgent.ContainsKey("MaxSubTasksPerRequest"))
                settings.MaxSubTasksPerRequest = Convert.ToInt32(multiAgent["MaxSubTasksPerRequest"]);
            
            if (multiAgent.ContainsKey("AgentTimeoutSeconds"))
                settings.AgentTimeoutSeconds = Convert.ToInt32(multiAgent["AgentTimeoutSeconds"]);
            
            if (multiAgent.ContainsKey("Models"))
            {
                var models = (Dictionary<object, object>)multiAgent["Models"];
                settings.Models = new ModelSettings
                {
                    DecompositionModel = models.ContainsKey("DecompositionModel") 
                        ? models["DecompositionModel"].ToString() ?? "gpt-4o-mini"
                        : "gpt-4o-mini",
                    AssemblyModel = models.ContainsKey("AssemblyModel")
                        ? models["AssemblyModel"].ToString() ?? "gpt-4o-mini"
                        : "gpt-4o-mini",
                    SpecialistModel = models.ContainsKey("SpecialistModel")
                        ? models["SpecialistModel"].ToString() ?? "gpt-4o"
                        : "gpt-4o"
                };
            }
            
            if (multiAgent.ContainsKey("Timeouts"))
            {
                var timeouts = (Dictionary<object, object>)multiAgent["Timeouts"];
                settings.Timeouts = new TimeoutSettings
                {
                    TaskDecompositionSeconds = timeouts.ContainsKey("TaskDecompositionSeconds")
                        ? Convert.ToInt32(timeouts["TaskDecompositionSeconds"])
                        : 10,
                    SpecialistSearchSeconds = timeouts.ContainsKey("SpecialistSearchSeconds")
                        ? Convert.ToInt32(timeouts["SpecialistSearchSeconds"])
                        : 15,
                    ScriptAssemblySeconds = timeouts.ContainsKey("ScriptAssemblySeconds")
                        ? Convert.ToInt32(timeouts["ScriptAssemblySeconds"])
                        : 10
                };
            }
            
            if (multiAgent.ContainsKey("Logging"))
            {
                var logging = (Dictionary<object, object>)multiAgent["Logging"];
                settings.Logging = new LoggingSettings
                {
                    LogDecomposition = logging.ContainsKey("LogDecomposition")
                        ? Convert.ToBoolean(logging["LogDecomposition"])
                        : true,
                    LogAgentExecution = logging.ContainsKey("LogAgentExecution")
                        ? Convert.ToBoolean(logging["LogAgentExecution"])
                        : true,
                    LogAssembly = logging.ContainsKey("LogAssembly")
                        ? Convert.ToBoolean(logging["LogAssembly"])
                        : true,
                    VerboseMetrics = logging.ContainsKey("VerboseMetrics")
                        ? Convert.ToBoolean(logging["VerboseMetrics"])
                        : true
                };
            }
        }
        
        return settings;
    }

    public static MultiAgentSettings GetDefaults()
    {
        return new MultiAgentSettings
        {
            MaxConcurrentAgents = 10,
            MaxSubTasksPerRequest = 20,
            AgentTimeoutSeconds = 30,
            Models = new ModelSettings
            {
                DecompositionModel = "gpt-4o-mini",
                AssemblyModel = "gpt-4o-mini",
                SpecialistModel = "gpt-4o"
            },
            Timeouts = new TimeoutSettings
            {
                TaskDecompositionSeconds = 10,
                SpecialistSearchSeconds = 15,
                ScriptAssemblySeconds = 10
            },
            Logging = new LoggingSettings
            {
                LogDecomposition = true,
                LogAgentExecution = true,
                LogAssembly = true,
                VerboseMetrics = true
            }
        };
    }
}
