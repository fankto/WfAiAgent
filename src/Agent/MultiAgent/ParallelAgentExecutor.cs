using System.Collections.Concurrent;
using Serilog;

namespace WorkflowPlus.AIAgent.MultiAgent;

/// <summary>
/// Coordinates parallel execution of specialist search agents.
/// Handles concurrency limits, resource management, and partial failures.
/// </summary>
public class ParallelAgentExecutor
{
    private readonly ILogger _logger;
    private readonly int _maxConcurrentAgents;

    public ParallelAgentExecutor(ILogger logger, int maxConcurrentAgents = 10)
    {
        _logger = logger;
        _maxConcurrentAgents = maxConcurrentAgents;
    }

    /// <summary>
    /// Executes N specialist agents in parallel (or single if N=1).
    /// Returns results from all agents, including failures.
    /// </summary>
    public async Task<List<SearchResult>> ExecuteAgentsAsync(
        List<SubTask> subtasks,
        Func<SubTask, Task<SearchResult>> agentFactory)
    {
        if (subtasks.Count == 0)
        {
            _logger.Warning("No subtasks to execute");
            return new List<SearchResult>();
        }

        if (subtasks.Count == 1)
        {
            _logger.Information("Executing single agent for 1 subtask");
            var result = await agentFactory(subtasks[0]);
            return new List<SearchResult> { result };
        }

        _logger.Information("Executing {Count} agents in parallel (max concurrent: {Max})",
            subtasks.Count, _maxConcurrentAgents);

        // If subtasks exceed max concurrent, batch them
        if (subtasks.Count > _maxConcurrentAgents)
        {
            return await ExecuteInBatchesAsync(subtasks, agentFactory);
        }

        // Execute all in parallel
        return await ExecuteAllParallelAsync(subtasks, agentFactory);
    }

    private async Task<List<SearchResult>> ExecuteAllParallelAsync(
        List<SubTask> subtasks,
        Func<SubTask, Task<SearchResult>> agentFactory)
    {
        var tasks = subtasks.Select(subtask => Task.Run(async () =>
        {
            try
            {
                return await agentFactory(subtask);
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Agent execution failed for subtask {Id}", subtask.Id);
                return new SearchResult
                {
                    SubTaskId = subtask.Id,
                    Success = false,
                    ErrorMessage = ex.Message
                };
            }
        })).ToList();

        var results = await Task.WhenAll(tasks);
        
        var successCount = results.Count(r => r.Success);
        var failureCount = results.Length - successCount;
        
        _logger.Information("Parallel execution complete: {Success} succeeded, {Failed} failed",
            successCount, failureCount);

        return results.ToList();
    }

    private async Task<List<SearchResult>> ExecuteInBatchesAsync(
        List<SubTask> subtasks,
        Func<SubTask, Task<SearchResult>> agentFactory)
    {
        _logger.Information("Batching {Total} subtasks into groups of {BatchSize}",
            subtasks.Count, _maxConcurrentAgents);

        var allResults = new ConcurrentBag<SearchResult>();
        var batches = subtasks
            .Select((subtask, index) => new { subtask, index })
            .GroupBy(x => x.index / _maxConcurrentAgents)
            .Select(g => g.Select(x => x.subtask).ToList())
            .ToList();

        for (int i = 0; i < batches.Count; i++)
        {
            _logger.Debug("Executing batch {Current}/{Total}", i + 1, batches.Count);
            
            var batchResults = await ExecuteAllParallelAsync(batches[i], agentFactory);
            foreach (var result in batchResults)
            {
                allResults.Add(result);
            }
        }

        return allResults.ToList();
    }

    /// <summary>
    /// Checks if partial failure threshold is exceeded.
    /// Returns true if >50% of agents failed.
    /// </summary>
    public bool ShouldAbortDueToFailures(List<SearchResult> results)
    {
        if (results.Count == 0) return true;

        var failureCount = results.Count(r => !r.Success);
        var failureRate = (double)failureCount / results.Count;

        if (failureRate > 0.5)
        {
            _logger.Warning("Failure rate {Rate:P0} exceeds threshold (>50%)", failureRate);
            return true;
        }

        return false;
    }
}
