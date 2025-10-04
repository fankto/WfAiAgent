using System.Diagnostics;
using Microsoft.SemanticKernel.ChatCompletion;
using Serilog;
using WorkflowPlus.AIAgent.Tools;

namespace WorkflowPlus.AIAgent.MultiAgent;

/// <summary>
/// Main orchestrator for multi-agent workflow.
/// Implements unified pattern: always decompose → spawn N agents → assemble.
/// </summary>
public class MultiAgentOrchestrator : IMultiAgentOrchestrator
{
    private readonly ITaskDecomposer _decomposer;
    private readonly IScriptAssembler _assembler;
    private readonly SearchKnowledgeTool _searchTool;
    private readonly IChatCompletionService _chatService;
    private readonly ParallelAgentExecutor _executor;
    private readonly MultiAgentSettings _settings;
    private readonly ILogger _logger;

    public MultiAgentOrchestrator(
        ITaskDecomposer decomposer,
        IScriptAssembler assembler,
        SearchKnowledgeTool searchTool,
        IChatCompletionService chatService,
        MultiAgentSettings settings,
        ILogger logger)
    {
        _decomposer = decomposer;
        _assembler = assembler;
        _searchTool = searchTool;
        _chatService = chatService;
        _settings = settings;
        _logger = logger;
        _executor = new ParallelAgentExecutor(logger, settings.MaxConcurrentAgents);
    }

    public async Task<OrchestrationResult> ProcessRequestAsync(string userRequest)
    {
        var totalStopwatch = Stopwatch.StartNew();
        var metrics = new OrchestrationMetrics();
        var errors = new List<string>();
        var warnings = new List<string>();

        _logger.Information("Starting multi-agent orchestration for request: {Request}", userRequest);

        try
        {
            // Step 1: Decompose task (always, returns N ≥ 1)
            var decompositionStopwatch = Stopwatch.StartNew();
            var decomposition = await _decomposer.DecomposeAsync(userRequest);
            decompositionStopwatch.Stop();
            metrics.DecompositionTime = decompositionStopwatch.Elapsed;

            if (!decomposition.Success)
            {
                _logger.Error("Task decomposition failed: {Error}", decomposition.ErrorMessage);
                return new OrchestrationResult
                {
                    Success = false,
                    Errors = new List<string> { decomposition.ErrorMessage },
                    Metrics = metrics
                };
            }

            metrics.SubTaskCount = decomposition.SubTasks.Count;
            _logger.Information("Decomposed into {Count} subtasks", decomposition.SubTasks.Count);

            if (_settings.Logging.LogDecomposition)
            {
                foreach (var subtask in decomposition.SubTasks)
                {
                    _logger.Debug("Subtask {Id}: {Description} (depends on: {Deps})",
                        subtask.Id, subtask.Description, string.Join(", ", subtask.DependsOn));
                }
            }

            // Step 2: Spawn N specialist agents (parallel if N>1)
            var searchStopwatch = Stopwatch.StartNew();
            var searchResults = await _executor.ExecuteAgentsAsync(
                decomposition.SubTasks,
                subtask => CreateAndExecuteAgentAsync(subtask)
            );
            searchStopwatch.Stop();
            metrics.SearchTime = searchStopwatch.Elapsed;

            // Check for excessive failures
            if (_executor.ShouldAbortDueToFailures(searchResults))
            {
                _logger.Error("Too many agent failures ({Failed}/{Total}), aborting",
                    searchResults.Count(r => !r.Success), searchResults.Count);
                
                return new OrchestrationResult
                {
                    Success = false,
                    Errors = new List<string> { "More than 50% of specialist agents failed" },
                    Metrics = metrics
                };
            }

            // Collect warnings for failed subtasks
            foreach (var result in searchResults.Where(r => !r.Success))
            {
                warnings.Add($"Subtask {result.SubTaskId} failed: {result.ErrorMessage}");
            }

            // Step 3: Aggregate results
            var commandsBySubtask = searchResults
                .Where(r => r.Success)
                .ToDictionary(r => r.SubTaskId, r => r.Commands);

            metrics.TotalCommandsFound = commandsBySubtask.Values.Sum(cmds => cmds.Count);
            _logger.Information("Found {Total} total commands across {Subtasks} subtasks",
                metrics.TotalCommandsFound, commandsBySubtask.Count);

            // Step 4: Assemble script
            var assemblyStopwatch = Stopwatch.StartNew();
            var assembly = await _assembler.AssembleScriptAsync(
                userRequest,
                decomposition.SubTasks,
                commandsBySubtask
            );
            assemblyStopwatch.Stop();
            metrics.AssemblyTime = assemblyStopwatch.Elapsed;

            if (!assembly.Success)
            {
                _logger.Error("Script assembly failed: {Error}", assembly.ErrorMessage);
                return new OrchestrationResult
                {
                    Success = false,
                    Errors = new List<string> { assembly.ErrorMessage },
                    Warnings = warnings,
                    Metrics = metrics
                };
            }

            warnings.AddRange(assembly.Warnings);

            // Complete
            totalStopwatch.Stop();
            metrics.TotalTime = totalStopwatch.Elapsed;
            metrics.EstimatedCost = CalculateEstimatedCost(metrics);

            if (_settings.Logging.VerboseMetrics)
            {
                LogMetrics(metrics);
            }

            _logger.Information("Multi-agent orchestration completed successfully in {Ms}ms",
                totalStopwatch.ElapsedMilliseconds);

            return new OrchestrationResult
            {
                Script = assembly.Script,
                Success = true,
                Metrics = metrics,
                Warnings = warnings
            };
        }
        catch (Exception ex)
        {
            totalStopwatch.Stop();
            _logger.Error(ex, "Multi-agent orchestration failed");
            
            return new OrchestrationResult
            {
                Success = false,
                Errors = new List<string> { $"Orchestration failed: {ex.Message}" },
                Metrics = metrics
            };
        }
    }

    private async Task<SearchResult> CreateAndExecuteAgentAsync(SubTask subtask)
    {
        var agent = new SpecialistSearchAgent(
            _searchTool,
            _logger,
            _settings.Timeouts.SpecialistSearchSeconds
        );

        return await agent.SearchForSubtaskAsync(subtask, maxCommands: 3);
    }

    private decimal CalculateEstimatedCost(OrchestrationMetrics metrics)
    {
        // Rough cost estimation
        // Decomposition: ~$0.002
        // Each agent search: ~$0.005
        // Assembly: ~$0.003
        
        decimal decompositionCost = 0.002m;
        decimal searchCost = metrics.SubTaskCount * 0.005m;
        decimal assemblyCost = 0.003m;

        return decompositionCost + searchCost + assemblyCost;
    }

    private void LogMetrics(OrchestrationMetrics metrics)
    {
        _logger.Information("=== Orchestration Metrics ===");
        _logger.Information("Subtasks: {Count}", metrics.SubTaskCount);
        _logger.Information("Commands found: {Count}", metrics.TotalCommandsFound);
        _logger.Information("Decomposition time: {Ms}ms", metrics.DecompositionTime.TotalMilliseconds);
        _logger.Information("Search time: {Ms}ms", metrics.SearchTime.TotalMilliseconds);
        _logger.Information("Assembly time: {Ms}ms", metrics.AssemblyTime.TotalMilliseconds);
        _logger.Information("Total time: {Ms}ms", metrics.TotalTime.TotalMilliseconds);
        _logger.Information("Estimated cost: ${Cost:F4}", metrics.EstimatedCost);
        _logger.Information("============================");
    }
}
