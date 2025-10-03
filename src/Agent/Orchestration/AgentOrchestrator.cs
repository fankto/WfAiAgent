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

        _kernel = builder.Build();
        _chatService = _kernel.GetRequiredService<IChatCompletionService>();

        // STATE-OF-THE-ART: Create query enhancer for advanced RAG
        var queryEnhancer = new QueryEnhancer(_chatService);

        // Register tools with advanced capabilities
        builder.Plugins.AddFromObject(new SearchKnowledgeTool(queryEnhancer), "SearchKnowledge");
        builder.Plugins.AddFromType<ExampleScriptMatcher>();
        builder.Plugins.AddFromType<ScriptValidationTool>();

        _kernel = builder.Build();

        // Initialize chat history with system prompt
        _history = new ChatHistory(_settings.SystemPrompt);

        _logger.Information("AgentOrchestrator initialized with model {Model} and advanced RAG techniques", _settings.DefaultModel);
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
    /// STATE-OF-THE-ART: Generate code with structured reflection.
    /// Uses JSON schema for reliable, machine-readable reviews.
    /// </summary>
    public async Task<CodeGenerationResult> GenerateCodeWithReflectionAsync(string userIntent)
    {
        _logger.Information("Generating code with structured reflection for: {Intent}", userIntent);

        // Step 1: Generate initial code
        var codePrompt = $"{_settings.CodeGenerationPrompt}\n\nUser request: {userIntent}";
        var initialCode = await InvokePromptAsync(codePrompt);

        // Step 2: Structured self-review loop
        for (int attempt = 0; attempt < _settings.MaxReflectionIterations; attempt++)
        {
            _logger.Debug("Reflection iteration {Iteration}", attempt + 1);

            var review = await GetStructuredCodeReviewAsync(initialCode);

            // Check approval with confidence threshold
            if (review.IsApproved && review.ConfidenceScore >= 80 && !review.HasCriticalIssues())
            {
                _logger.Information("Code approved after {Iterations} iterations (Score: {Score}, Issues: {Issues})",
                    attempt + 1, review.OverallQualityScore, review.TotalIssueCount());
                return CodeGenerationResult.Success(initialCode, attempt + 1);
            }

            // Log issues for debugging
            _logger.Information("Review found {IssueCount} issues (Confidence: {Confidence})",
                review.TotalIssueCount(), review.ConfidenceScore);

            if (review.HasCriticalIssues())
            {
                _logger.Warning("Critical issues found: {CriticalCount}",
                    review.SyntaxErrors.Count(e => e.Severity == "critical"));
            }

            // Apply fixes based on structured feedback
            initialCode = await ApplyStructuredFixesAsync(initialCode, review);
        }

        _logger.Warning("Max reflection iterations reached");
        return CodeGenerationResult.WithWarning(initialCode,
            "Max reflection iterations reached. Code may need manual review.");
    }

    private async Task<CodeReview> GetStructuredCodeReviewAsync(string code)
    {
        var reviewPrompt = $@"{_settings.ReflectionPrompt}

Code to review:
```
{code}
```

Return a JSON object with this exact structure:
{{
  ""is_approved"": true/false,
  ""confidence_score"": 0-100,
  ""syntax_errors"": [{{""line"": 5, ""severity"": ""error"", ""description"": ""...", ""suggested_fix"": ""...""}}],
  ""logic_issues"": [],
  ""best_practice_violations"": [],
  ""security_concerns"": [],
  ""suggested_improvements"": [""...""],
  ""overall_quality_score"": 0-100
}}

JSON:";

        try
        {
            var response = await InvokePromptAsync(reviewPrompt);

            // Extract JSON from response (handles markdown code blocks)
            var jsonStart = response.IndexOf('{');
            var jsonEnd = response.LastIndexOf('}');
            if (jsonStart >= 0 && jsonEnd > jsonStart)
            {
                var json = response.Substring(jsonStart, jsonEnd - jsonStart + 1);
                var review = System.Text.Json.JsonSerializer.Deserialize<CodeReview>(json);
                if (review != null)
                    return review;
            }

            _logger.Warning("Failed to parse structured review, using fallback");
            return new CodeReview
            {
                IsApproved = false,
                ConfidenceScore = 50,
                LogicIssues = new List<CodeIssue>
                {
                    new CodeIssue { Severity = "warning", Description = "Unable to perform structured review" }
                },
                OverallQualityScore = 50
            };
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Error getting structured review");
            return new CodeReview { IsApproved = false, ConfidenceScore = 0, OverallQualityScore = 0 };
        }
    }

    private async Task<string> ApplyStructuredFixesAsync(string code, CodeReview review)
    {
        var issuesDescription = new StringBuilder();

        if (review.SyntaxErrors.Any())
        {
            issuesDescription.AppendLine("Syntax Errors:");
            foreach (var error in review.SyntaxErrors)
            {
                issuesDescription.AppendLine($"  - Line {error.Line}: {error.Description}");
                if (!string.IsNullOrEmpty(error.SuggestedFix))
                    issuesDescription.AppendLine($"    Fix: {error.SuggestedFix}");
            }
        }

        if (review.LogicIssues.Any())
        {
            issuesDescription.AppendLine("Logic Issues:");
            foreach (var issue in review.LogicIssues)
            {
                issuesDescription.AppendLine($"  - {issue.Description}");
            }
        }

        if (review.SecurityConcerns.Any())
        {
            issuesDescription.AppendLine("Security Concerns:");
            foreach (var concern in review.SecurityConcerns)
            {
                issuesDescription.AppendLine($"  - {concern.Description}");
            }
        }

        var fixPrompt = $@"Fix the following issues in the code:

{issuesDescription}

Original code:
```
{code}
```

Provide the corrected code:";

        return await InvokePromptAsync(fixPrompt);
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
