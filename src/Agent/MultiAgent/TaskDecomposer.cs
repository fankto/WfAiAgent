using System.Text.Json;
using Microsoft.SemanticKernel.ChatCompletion;
using Serilog;

namespace WorkflowPlus.AIAgent.MultiAgent;

/// <summary>
/// Decomposes user requests into atomic subtasks using LLM.
/// Handles both simple (N=1) and complex (N>1) requests naturally.
/// </summary>
public class TaskDecomposer : ITaskDecomposer
{
    private readonly IChatCompletionService _chatService;
    private readonly ILogger _logger;

    public TaskDecomposer(IChatCompletionService chatService, ILogger logger)
    {
        _chatService = chatService;
        _logger = logger;
    }

    public async Task<DecompositionResult> DecomposeAsync(string userRequest)
    {
        _logger.Information("Decomposing user request: {Request}", userRequest);

        try
        {
            var prompt = BuildDecompositionPrompt(userRequest);
            var response = await _chatService.GetChatMessageContentAsync(prompt);
            var content = response.Content?.Trim() ?? "";

            var decomposition = ParseDecompositionResponse(content);
            
            if (decomposition.Success)
            {
                ValidateDependencies(decomposition);
                _logger.Information("Decomposed into {Count} subtasks", decomposition.SubTasks.Count);
            }

            return decomposition;
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Error decomposing task");
            return new DecompositionResult
            {
                Success = false,
                ErrorMessage = $"Task decomposition failed: {ex.Message}"
            };
        }
    }

    private string BuildDecompositionPrompt(string userRequest)
    {
        return $@"Decompose the following user request into atomic subtasks.

User request: ""{userRequest}""

Rules:
- For SIMPLE requests (single operation), return ONE subtask
- For COMPLEX requests (multiple operations), return MULTIPLE subtasks
- Each subtask should be a single, focused operation
- Identify dependencies between subtasks (which must complete before others)
- Number subtasks starting from 1

Return ONLY a JSON object with this structure:
{{
  ""subtasks"": [
    {{
      ""id"": 1,
      ""description"": ""Brief description of what this subtask does"",
      ""depends_on"": []
    }},
    {{
      ""id"": 2,
      ""description"": ""Another subtask"",
      ""depends_on"": [1]
    }}
  ]
}}

Examples:

Simple request: ""Create an array""
{{
  ""subtasks"": [
    {{""id"": 1, ""description"": ""Create an array"", ""depends_on"": []}}
  ]
}}

Complex request: ""Create a list, sort it, and save to file""
{{
  ""subtasks"": [
    {{""id"": 1, ""description"": ""Create and populate array"", ""depends_on"": []}},
    {{""id"": 2, ""description"": ""Sort array alphabetically"", ""depends_on"": [1]}},
    {{""id"": 3, ""description"": ""Write array to file"", ""depends_on"": [2]}}
  ]
}}

JSON:";
    }

    private DecompositionResult ParseDecompositionResponse(string content)
    {
        try
        {
            // Extract JSON from response (handle markdown code blocks)
            var jsonStart = content.IndexOf('{');
            var jsonEnd = content.LastIndexOf('}');
            
            if (jsonStart < 0 || jsonEnd <= jsonStart)
            {
                return new DecompositionResult
                {
                    Success = false,
                    ErrorMessage = "No valid JSON found in response"
                };
            }

            var json = content.Substring(jsonStart, jsonEnd - jsonStart + 1);
            var parsed = JsonSerializer.Deserialize<DecompositionResponse>(json, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            if (parsed?.Subtasks == null || parsed.Subtasks.Count == 0)
            {
                return new DecompositionResult
                {
                    Success = false,
                    ErrorMessage = "No subtasks found in response"
                };
            }

            // Convert to SubTask objects
            var subtasks = parsed.Subtasks.Select(st => new SubTask
            {
                Id = st.Id,
                Description = st.Description,
                DependsOn = st.DependsOn ?? new List<int>()
            }).ToList();

            // Build dependency dictionary
            var dependencies = subtasks.ToDictionary(
                st => st.Id,
                st => st.DependsOn
            );

            return new DecompositionResult
            {
                SubTasks = subtasks,
                Dependencies = dependencies,
                Success = true
            };
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Error parsing decomposition response");
            return new DecompositionResult
            {
                Success = false,
                ErrorMessage = $"Failed to parse response: {ex.Message}"
            };
        }
    }

    private void ValidateDependencies(DecompositionResult result)
    {
        var subtaskIds = result.SubTasks.Select(st => st.Id).ToHashSet();

        foreach (var subtask in result.SubTasks)
        {
            // Check all dependencies exist
            foreach (var depId in subtask.DependsOn)
            {
                if (!subtaskIds.Contains(depId))
                {
                    _logger.Warning("Subtask {Id} depends on non-existent subtask {DepId}", 
                        subtask.Id, depId);
                }
            }

            // Check for circular dependencies (simple check)
            if (HasCircularDependency(subtask.Id, result.Dependencies, new HashSet<int>()))
            {
                _logger.Warning("Circular dependency detected for subtask {Id}", subtask.Id);
            }
        }
    }

    private bool HasCircularDependency(int subtaskId, Dictionary<int, List<int>> dependencies, HashSet<int> visited)
    {
        if (visited.Contains(subtaskId))
            return true;

        visited.Add(subtaskId);

        if (dependencies.TryGetValue(subtaskId, out var deps))
        {
            foreach (var depId in deps)
            {
                if (HasCircularDependency(depId, dependencies, new HashSet<int>(visited)))
                    return true;
            }
        }

        return false;
    }

    // DTOs for JSON parsing
    private class DecompositionResponse
    {
        public List<SubTaskDto> Subtasks { get; set; } = new();
    }

    private class SubTaskDto
    {
        public int Id { get; set; }
        public string Description { get; set; } = string.Empty;
        public List<int>? DependsOn { get; set; }
    }
}
