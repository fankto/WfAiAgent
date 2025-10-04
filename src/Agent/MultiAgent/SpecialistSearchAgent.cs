using System.Diagnostics;
using Microsoft.SemanticKernel.ChatCompletion;
using Serilog;
using WorkflowPlus.AIAgent.Tools;

namespace WorkflowPlus.AIAgent.MultiAgent;

/// <summary>
/// Specialist agent that performs focused search for a single subtask.
/// Uses existing SearchKnowledgeTool with HyDE and query expansion.
/// </summary>
public class SpecialistSearchAgent : ISpecialistSearchAgent
{
    private readonly SearchKnowledgeTool _searchTool;
    private readonly ILogger _logger;
    private readonly int _timeoutSeconds;

    public SpecialistSearchAgent(
        SearchKnowledgeTool searchTool,
        ILogger logger,
        int timeoutSeconds = 15)
    {
        _searchTool = searchTool;
        _logger = logger;
        _timeoutSeconds = timeoutSeconds;
    }

    public async Task<SearchResult> SearchForSubtaskAsync(SubTask subtask, int maxCommands = 3)
    {
        var stopwatch = Stopwatch.StartNew();
        _logger.Debug("Specialist agent searching for subtask {Id}: {Description}", 
            subtask.Id, subtask.Description);

        try
        {
            subtask.Status = SubTaskStatus.InProgress;

            // Use timeout to prevent hanging
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(_timeoutSeconds));
            
            // Search using only the subtask description (no full context)
            var searchTask = _searchTool.SearchCommandsAsync(subtask.Description, maxCommands);
            var searchResultText = await searchTask.WaitAsync(cts.Token);

            // Parse commands from search result
            var commands = ParseCommandsFromSearchResult(searchResultText);

            stopwatch.Stop();
            subtask.Status = SubTaskStatus.Completed;

            _logger.Information("Specialist agent found {Count} commands for subtask {Id} in {Ms}ms",
                commands.Count, subtask.Id, stopwatch.ElapsedMilliseconds);

            return new SearchResult
            {
                SubTaskId = subtask.Id,
                Commands = commands,
                Success = true,
                ExecutionTime = stopwatch.Elapsed
            };
        }
        catch (OperationCanceledException)
        {
            stopwatch.Stop();
            subtask.Status = SubTaskStatus.Failed;
            
            _logger.Warning("Specialist agent timed out for subtask {Id} after {Seconds}s",
                subtask.Id, _timeoutSeconds);

            return new SearchResult
            {
                SubTaskId = subtask.Id,
                Success = false,
                ErrorMessage = $"Search timed out after {_timeoutSeconds} seconds",
                ExecutionTime = stopwatch.Elapsed
            };
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            subtask.Status = SubTaskStatus.Failed;
            
            _logger.Error(ex, "Specialist agent failed for subtask {Id}", subtask.Id);

            return new SearchResult
            {
                SubTaskId = subtask.Id,
                Success = false,
                ErrorMessage = ex.Message,
                ExecutionTime = stopwatch.Elapsed
            };
        }
    }

    private List<CommandMatch> ParseCommandsFromSearchResult(string searchResultText)
    {
        // The SearchKnowledgeTool returns formatted text with command details
        // We need to extract CommandMatch objects from it
        // For now, return empty list - this will be populated by the actual search tool
        // In a real implementation, we'd parse the markdown-formatted result
        
        // TODO: Implement proper parsing or modify SearchKnowledgeTool to return structured data
        return new List<CommandMatch>();
    }
}
