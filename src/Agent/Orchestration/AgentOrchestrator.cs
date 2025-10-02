using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using Serilog;
using WorkflowPlus.AIAgent.Core.Models;
using WorkflowPlus.AIAgent.Tools;

namespace WorkflowPlus.AIAgent.Orchestration;

/// <summary>
/// Main orchestrator for the AI agent system.
/// Coordinates between LLM, tools, and conversation management.
/// </summary>
public class AgentOrchestrator
{
    private readonly Kernel _kernel;
    private readonly ChatHistory _history;
    private readonly AgentSettings _settings;
    private readonly ILogger _logger;
    private readonly IChatCompletionService _chatService;

    public AgentOrchestrator(string apiKey, AgentSettings settings, ILogger logger)
    {
        _settings = settings;
        _logger = logger;

        // Build Semantic Kernel
        var builder = Kernel.CreateBuilder();
        builder.AddOpenAIChatCompletion(_settings.DefaultModel, apiKey);
        
        // Register tools
        builder.Plugins.AddFromType<SearchKnowledgeTool>();
        builder.Plugins.AddFromType<ScriptValidationTool>();
        
        _kernel = builder.Build();
        _chatService = _kernel.GetRequiredService<IChatCompletionService>();
        
        // Initialize chat history with system prompt
        _history = new ChatHistory(_settings.SystemPrompt);
        
        _logger.Information("AgentOrchestrator initialized with model {Model}", _settings.DefaultModel);
    }

    /// <summary>
    /// Process a user query and return a response.
    /// </summary>
    public async Task<AgentResponse> ProcessQueryAsync(string userQuery, string? conversationId = null)
    {
        _logger.Information("Processing query: {Query}", userQuery);
        
        var startTime = DateTime.UtcNow;
        var toolCalls = new List<ToolCallResult>();

        try
        {
            // Add user message to history
            _history.AddUserMessage(userQuery);

            // Configure execution settings with tool calling
            var executionSettings = new OpenAIPromptExecutionSettings
            {
                ToolCallBehavior = ToolCallBehavior.AutoInvokeKernelFunctions,
                Temperature = _settings.Temperature,
                MaxTokens = _settings.MaxTokens
            };

            // Get response from LLM
            var result = await _chatService.GetChatMessageContentAsync(
                _history,
                executionSettings,
                _kernel
            );

            // Add assistant response to history
            _history.AddAssistantMessage(result.Content ?? string.Empty);

            // Extract token usage
            var usage = result.Metadata?["Usage"] as dynamic;
            int totalTokens = usage?.TotalTokens ?? 0;
            
            // Calculate cost (approximate)
            decimal cost = CalculateCost(totalTokens, _settings.DefaultModel);

            _logger.Information("Query processed successfully. Tokens: {Tokens}, Cost: ${Cost:F4}", 
                totalTokens, cost);

            return new AgentResponse
            {
                Content = result.Content ?? string.Empty,
                ConversationId = conversationId,
                TokensUsed = totalTokens,
                EstimatedCost = cost,
                ModelUsed = _settings.DefaultModel,
                ToolCalls = toolCalls,
                Success = true
            };
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Error processing query: {Query}", userQuery);
            
            return new AgentResponse
            {
                Content = "I encountered an error processing your request. Please try again.",
                Success = false,
                ErrorMessage = ex.Message
            };
        }
    }

    /// <summary>
    /// Stream response tokens in real-time.
    /// </summary>
    public async IAsyncEnumerable<string> StreamResponseAsync(string userQuery)
    {
        _logger.Information("Streaming response for query: {Query}", userQuery);
        
        _history.AddUserMessage(userQuery);

        var executionSettings = new OpenAIPromptExecutionSettings
        {
            ToolCallBehavior = ToolCallBehavior.AutoInvokeKernelFunctions,
            Temperature = _settings.Temperature,
            MaxTokens = _settings.MaxTokens
        };

        var stream = _chatService.GetStreamingChatMessageContentsAsync(
            _history,
            executionSettings,
            _kernel
        );

        var fullResponse = string.Empty;

        await foreach (var chunk in stream)
        {
            if (!string.IsNullOrEmpty(chunk.Content))
            {
                fullResponse += chunk.Content;
                yield return chunk.Content;
            }
        }

        // Add complete response to history
        _history.AddAssistantMessage(fullResponse);
    }

    /// <summary>
    /// Generate code with reflection (self-review).
    /// </summary>
    public async Task<CodeGenerationResult> GenerateCodeWithReflectionAsync(string userIntent)
    {
        _logger.Information("Generating code with reflection for: {Intent}", userIntent);

        // Step 1: Generate initial code
        var codePrompt = $"{_settings.CodeGenerationPrompt}\n\nUser request: {userIntent}";
        var initialCode = await InvokePromptAsync(codePrompt);

        // Step 2: Self-review loop
        for (int attempt = 0; attempt < _settings.MaxReflectionIterations; attempt++)
        {
            _logger.Debug("Reflection iteration {Iteration}", attempt + 1);

            var reviewPrompt = $@"{_settings.ReflectionPrompt}

Code to review:
```
{initialCode}
```

Check for errors and respond with either 'APPROVED' or specific fixes needed.";

            var review = await InvokePromptAsync(reviewPrompt);

            if (review.Contains("APPROVED", StringComparison.OrdinalIgnoreCase))
            {
                _logger.Information("Code approved after {Iterations} iterations", attempt + 1);
                return CodeGenerationResult.Success(initialCode, attempt + 1);
            }

            // Apply fixes
            var fixPrompt = $@"Apply these fixes to the code:

{review}

Original code:
```
{initialCode}
```

Provide the corrected code:";

            initialCode = await InvokePromptAsync(fixPrompt);
        }

        _logger.Warning("Max reflection iterations reached");
        return CodeGenerationResult.WithWarning(initialCode, "Max reflection iterations reached. Please review carefully.");
    }

    private async Task<string> InvokePromptAsync(string prompt)
    {
        var tempHistory = new ChatHistory(prompt);
        var result = await _chatService.GetChatMessageContentAsync(tempHistory);
        return result.Content ?? string.Empty;
    }

    private decimal CalculateCost(int tokens, string model)
    {
        // Approximate costs (as of 2025)
        // GPT-4o: $2.50/M input, $10/M output (average $6/M)
        // GPT-4o-mini: $0.15/M input, $0.60/M output (average $0.375/M)
        
        decimal costPerMillion = model.Contains("mini") ? 0.375m : 6.0m;
        return (tokens / 1_000_000m) * costPerMillion;
    }

    public void ClearHistory()
    {
        _history.Clear();
        _history.AddSystemMessage(_settings.SystemPrompt);
    }
}
