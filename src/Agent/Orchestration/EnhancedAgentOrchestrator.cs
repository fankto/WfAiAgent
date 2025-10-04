using Microsoft.SemanticKernel.ChatCompletion;
using Serilog;
using WorkflowPlus.AIAgent.Core.Models;
using WorkflowPlus.AIAgent.MultiAgent;
using WorkflowPlus.AIAgent.Tools;

namespace WorkflowPlus.AIAgent.Orchestration;

/// <summary>
/// Enhanced orchestrator that uses multi-agent pattern for script generation.
/// Replaces the existing single-agent approach with unified multi-agent orchestration.
/// </summary>
public class EnhancedAgentOrchestrator
{
    private readonly IMultiAgentOrchestrator _multiAgentOrchestrator;
    private readonly AgentSettings _settings;
    private readonly ILogger _logger;

    public EnhancedAgentOrchestrator(
        string apiKey,
        AgentSettings settings,
        MultiAgentSettings multiAgentSettings,
        ILogger logger)
    {
        _settings = settings;
        _logger = logger;

        // Build multi-agent orchestrator
        var chatService = CreateChatService(apiKey, settings);
        var queryEnhancer = new QueryEnhancer(chatService);
        var searchTool = new SearchKnowledgeTool(queryEnhancer, chatService);
        
        var decomposer = new TaskDecomposer(chatService, logger);
        var assembler = new ScriptAssembler(chatService, logger);

        _multiAgentOrchestrator = new MultiAgentOrchestrator(
            decomposer,
            assembler,
            searchTool,
            chatService,
            multiAgentSettings,
            logger
        );

        _logger.Information("EnhancedAgentOrchestrator initialized with multi-agent pattern");
    }

    /// <summary>
    /// Process a user query using multi-agent orchestration.
    /// Always uses the unified pattern (decompose → spawn agents → assemble).
    /// </summary>
    public async Task<AgentResponse> ProcessQueryAsync(string userQuery, string? conversationId = null)
    {
        _logger.Information("Processing query with multi-agent orchestration: {Query}", userQuery);
        
        var startTime = DateTime.UtcNow;

        try
        {
            // Use multi-agent orchestration
            var result = await _multiAgentOrchestrator.ProcessRequestAsync(userQuery);

            if (!result.Success)
            {
                _logger.Error("Multi-agent orchestration failed: {Errors}", 
                    string.Join(", ", result.Errors));
                
                return new AgentResponse
                {
                    Content = $"Failed to process request: {string.Join(", ", result.Errors)}",
                    Success = false,
                    ErrorMessage = string.Join(", ", result.Errors)
                };
            }

            var endTime = DateTime.UtcNow;
            var totalTime = (endTime - startTime).TotalSeconds;

            _logger.Information("Query processed successfully in {Seconds}s. Cost: ${Cost:F4}", 
                totalTime, result.Metrics.EstimatedCost);

            // Log warnings if any
            if (result.Warnings.Any())
            {
                foreach (var warning in result.Warnings)
                {
                    _logger.Warning("Orchestration warning: {Warning}", warning);
                }
            }

            return new AgentResponse
            {
                Content = result.Script,
                ConversationId = conversationId,
                TokensUsed = 0, // TODO: Extract from metrics
                EstimatedCost = result.Metrics.EstimatedCost,
                ModelUsed = _settings.DefaultModel,
                ToolCalls = new List<ToolCallResult>(),
                Success = true,
                Metadata = new Dictionary<string, object>
                {
                    ["SubTaskCount"] = result.Metrics.SubTaskCount,
                    ["TotalCommandsFound"] = result.Metrics.TotalCommandsFound,
                    ["DecompositionTime"] = result.Metrics.DecompositionTime.TotalMilliseconds,
                    ["SearchTime"] = result.Metrics.SearchTime.TotalMilliseconds,
                    ["AssemblyTime"] = result.Metrics.AssemblyTime.TotalMilliseconds,
                    ["TotalTime"] = result.Metrics.TotalTime.TotalMilliseconds,
                    ["Warnings"] = result.Warnings
                }
            };
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Error processing query with multi-agent orchestration");
            
            return new AgentResponse
            {
                Content = "An error occurred processing your request.",
                Success = false,
                ErrorMessage = ex.Message
            };
        }
    }

    private IChatCompletionService CreateChatService(string apiKey, AgentSettings settings)
    {
        // Create a simple chat service
        // In production, this would use the full Semantic Kernel setup
        var builder = Microsoft.SemanticKernel.Kernel.CreateBuilder();
        builder.AddOpenAIChatCompletion(settings.DefaultModel, apiKey);
        var kernel = builder.Build();
        return kernel.GetRequiredService<IChatCompletionService>();
    }
}
