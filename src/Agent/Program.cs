using Serilog;
using WorkflowPlus.AIAgent.Orchestration;

namespace WorkflowPlus.AIAgent;

/// <summary>
/// Console test application for the AI Agent.
/// This is a temporary test harness - the actual plugin will integrate with WinForms.
/// </summary>
class Program
{
    static async Task Main(string[] args)
    {
        // Configure logging
        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Debug()
            .WriteTo.Console()
            .WriteTo.File("logs/agent-.log", rollingInterval: RollingInterval.Day)
            .CreateLogger();

        try
        {
            Console.WriteLine("=== Workflow+ AI Agent Test Console ===\n");

            // Get API key from environment
            var apiKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY");
            if (string.IsNullOrEmpty(apiKey))
            {
                Console.WriteLine("ERROR: OPENAI_API_KEY environment variable not set.");
                Console.WriteLine("Please set it with: export OPENAI_API_KEY='your-key-here'");
                return;
            }

            // Load settings
            var settingsPath = Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", "agent_config.yml");
            var settings = AgentSettings.LoadFromYaml(settingsPath);

            // Create orchestrator
            var orchestrator = new AgentOrchestrator(apiKey, settings, Log.Logger);

            Console.WriteLine("Agent initialized successfully!");
            Console.WriteLine($"Model: {settings.DefaultModel}");
            Console.WriteLine($"Max cost per query: ${settings.MaxCostPerQuery}");
            Console.WriteLine("\nType your questions below. Type 'exit' to quit, 'clear' to reset conversation.\n");

            // Interactive loop
            while (true)
            {
                Console.Write("\nYou: ");
                var input = Console.ReadLine();

                if (string.IsNullOrWhiteSpace(input))
                    continue;

                if (input.Equals("exit", StringComparison.OrdinalIgnoreCase))
                    break;

                if (input.Equals("clear", StringComparison.OrdinalIgnoreCase))
                {
                    orchestrator.ClearHistory();
                    Console.WriteLine("Conversation cleared.");
                    continue;
                }

                Console.Write("\nAgent: ");

                // Stream response
                await foreach (var token in orchestrator.StreamResponseAsync(input))
                {
                    Console.Write(token);
                }

                Console.WriteLine();
            }

            Console.WriteLine("\nGoodbye!");
        }
        catch (Exception ex)
        {
            Log.Fatal(ex, "Application terminated unexpectedly");
            Console.WriteLine($"\nFATAL ERROR: {ex.Message}");
        }
        finally
        {
            Log.CloseAndFlush();
        }
    }
}
