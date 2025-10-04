using System.Text;
using Microsoft.SemanticKernel.ChatCompletion;
using Serilog;
using WorkflowPlus.AIAgent.Tools;

namespace WorkflowPlus.AIAgent.MultiAgent;

/// <summary>
/// Assembles commands from subtasks into a coherent script.
/// Handles both trivial (N=1) and complex (N>1) assembly.
/// </summary>
public class ScriptAssembler : IScriptAssembler
{
    private readonly IChatCompletionService _chatService;
    private readonly ILogger _logger;

    public ScriptAssembler(IChatCompletionService chatService, ILogger logger)
    {
        _chatService = chatService;
        _logger = logger;
    }

    public async Task<AssemblyResult> AssembleScriptAsync(
        string userRequest,
        List<SubTask> subtasks,
        Dictionary<int, List<CommandMatch>> commandsBySubtask)
    {
        _logger.Information("Assembling script for {Count} subtasks", subtasks.Count);

        try
        {
            // Trivial case: single subtask
            if (subtasks.Count == 1)
            {
                return AssembleTrivialScript(subtasks[0], commandsBySubtask);
            }

            // Complex case: multiple subtasks with dependencies
            return await AssembleComplexScriptAsync(userRequest, subtasks, commandsBySubtask);
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Error assembling script");
            return new AssemblyResult
            {
                Success = false,
                ErrorMessage = $"Script assembly failed: {ex.Message}"
            };
        }
    }

    private AssemblyResult AssembleTrivialScript(
        SubTask subtask,
        Dictionary<int, List<CommandMatch>> commandsBySubtask)
    {
        _logger.Debug("Performing trivial assembly for single subtask");

        if (!commandsBySubtask.TryGetValue(subtask.Id, out var commands) || commands.Count == 0)
        {
            return new AssemblyResult
            {
                Success = false,
                ErrorMessage = "No commands found for subtask"
            };
        }

        // For single subtask, just format the commands simply
        var script = new StringBuilder();
        foreach (var command in commands)
        {
            script.AppendLine($"// {command.Description}");
            if (!string.IsNullOrEmpty(command.Syntax))
            {
                script.AppendLine(command.Syntax);
            }
            else
            {
                script.AppendLine($"{command.Name}();");
            }
            script.AppendLine();
        }

        return new AssemblyResult
        {
            Script = script.ToString().Trim(),
            Success = true
        };
    }

    private async Task<AssemblyResult> AssembleComplexScriptAsync(
        string userRequest,
        List<SubTask> subtasks,
        Dictionary<int, List<CommandMatch>> commandsBySubtask)
    {
        _logger.Debug("Performing complex assembly for {Count} subtasks", subtasks.Count);

        // Build prompt with all context
        var prompt = BuildAssemblyPrompt(userRequest, subtasks, commandsBySubtask);
        
        var response = await _chatService.GetChatMessageContentAsync(prompt);
        var script = response.Content?.Trim() ?? "";

        // Extract code from markdown if present
        script = ExtractCodeFromMarkdown(script);

        // Validate script
        var warnings = ValidateScript(subtasks, commandsBySubtask, script);

        return new AssemblyResult
        {
            Script = script,
            Success = true,
            Warnings = warnings
        };
    }

    private string BuildAssemblyPrompt(
        string userRequest,
        List<SubTask> subtasks,
        Dictionary<int, List<CommandMatch>> commandsBySubtask)
    {
        var prompt = new StringBuilder();
        
        prompt.AppendLine("Generate a complete, working script for the following user request:");
        prompt.AppendLine($"\nUser request: \"{userRequest}\"");
        prompt.AppendLine("\nThe request has been broken down into subtasks with commands found for each:");
        prompt.AppendLine();

        // Add each subtask with its commands
        foreach (var subtask in subtasks.OrderBy(st => st.Id))
        {
            prompt.AppendLine($"Subtask {subtask.Id}: {subtask.Description}");
            
            if (subtask.DependsOn.Any())
            {
                prompt.AppendLine($"  Dependencies: {string.Join(", ", subtask.DependsOn)}");
            }

            if (commandsBySubtask.TryGetValue(subtask.Id, out var commands) && commands.Any())
            {
                prompt.AppendLine("  Available commands:");
                foreach (var cmd in commands)
                {
                    prompt.AppendLine($"    - {cmd.Name}: {cmd.Description}");
                    if (!string.IsNullOrEmpty(cmd.Syntax))
                    {
                        prompt.AppendLine($"      Syntax: {cmd.Syntax}");
                    }
                    if (!string.IsNullOrEmpty(cmd.Parameters))
                    {
                        prompt.AppendLine($"      Parameters: {cmd.Parameters}");
                    }
                }
            }
            else
            {
                prompt.AppendLine("  WARNING: No commands found for this subtask");
            }
            
            prompt.AppendLine();
        }

        prompt.AppendLine("Requirements:");
        prompt.AppendLine("1. Generate a complete, executable script");
        prompt.AppendLine("2. Respect the dependency order between subtasks");
        prompt.AppendLine("3. Ensure proper variable flow between steps");
        prompt.AppendLine("4. Include error handling where appropriate");
        prompt.AppendLine("5. Add brief comments explaining each major step");
        prompt.AppendLine("6. Use the provided command syntax");
        prompt.AppendLine();
        prompt.AppendLine("Return ONLY the script code, no explanations:");

        return prompt.ToString();
    }

    private string ExtractCodeFromMarkdown(string content)
    {
        // Remove markdown code blocks if present
        var codeBlockStart = content.IndexOf("```");
        if (codeBlockStart >= 0)
        {
            var codeStart = content.IndexOf('\n', codeBlockStart) + 1;
            var codeEnd = content.IndexOf("```", codeStart);
            if (codeEnd > codeStart)
            {
                return content.Substring(codeStart, codeEnd - codeStart).Trim();
            }
        }
        
        return content;
    }

    private List<string> ValidateScript(
        List<SubTask> subtasks,
        Dictionary<int, List<CommandMatch>> commandsBySubtask,
        string script)
    {
        var warnings = new List<string>();

        // Check if all subtasks are represented
        foreach (var subtask in subtasks)
        {
            if (!commandsBySubtask.ContainsKey(subtask.Id) || 
                commandsBySubtask[subtask.Id].Count == 0)
            {
                warnings.Add($"Subtask {subtask.Id} had no commands available");
            }
        }

        // Check if script is empty
        if (string.IsNullOrWhiteSpace(script))
        {
            warnings.Add("Generated script is empty");
        }

        return warnings;
    }
}
