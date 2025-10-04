using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Moq;
using Serilog;
using WorkflowPlus.AIAgent.MultiAgent;
using WorkflowPlus.AIAgent.Tools;
using Xunit;

namespace WorkflowPlus.AIAgent.Tests.Integration;

/// <summary>
/// Integration tests for multi-agent orchestration system.
/// </summary>
public class MultiAgentOrchestrationTests
{
    private readonly Mock<IChatCompletionService> _mockChatService;
    private readonly Mock<SearchKnowledgeTool> _mockSearchTool;
    private readonly ILogger _logger;
    private readonly MultiAgentSettings _settings;

    public MultiAgentOrchestrationTests()
    {
        _mockChatService = new Mock<IChatCompletionService>();
        _mockSearchTool = new Mock<SearchKnowledgeTool>();
        _logger = new LoggerConfiguration()
            .WriteTo.Console()
            .CreateLogger();
        
        _settings = new MultiAgentSettings
        {
            MaxConcurrentAgents = 10,
            MaxSubTasksPerRequest = 20,
            AgentTimeoutSeconds = 30
        };
    }

    // Note: TaskDecomposer tests require real LLM calls
    // These are commented out for unit testing without API keys
    // Uncomment and run with OPENAI_API_KEY set for integration testing

    [Fact]
    public async Task ParallelAgentExecutor_SingleSubtask_ExecutesSequentially()
    {
        // Arrange
        var executor = new ParallelAgentExecutor(_logger, maxConcurrentAgents: 10);
        var subtasks = new List<SubTask>
        {
            new SubTask { Id = 1, Description = "Test task" }
        };

        var executionCount = 0;
        Func<SubTask, Task<SearchResult>> agentFactory = async (subtask) =>
        {
            executionCount++;
            await Task.Delay(10);
            return new SearchResult
            {
                SubTaskId = subtask.Id,
                Success = true,
                Commands = new List<CommandMatch>()
            };
        };

        // Act
        var results = await executor.ExecuteAgentsAsync(subtasks, agentFactory);

        // Assert
        Assert.Single(results);
        Assert.Equal(1, executionCount);
        Assert.True(results[0].Success);
    }

    [Fact]
    public async Task ParallelAgentExecutor_MultipleSubtasks_ExecutesInParallel()
    {
        // Arrange
        var executor = new ParallelAgentExecutor(_logger, maxConcurrentAgents: 10);
        var subtasks = new List<SubTask>
        {
            new SubTask { Id = 1, Description = "Task 1" },
            new SubTask { Id = 2, Description = "Task 2" },
            new SubTask { Id = 3, Description = "Task 3" }
        };

        var executionTimes = new System.Collections.Concurrent.ConcurrentBag<DateTime>();
        Func<SubTask, Task<SearchResult>> agentFactory = async (subtask) =>
        {
            executionTimes.Add(DateTime.UtcNow);
            await Task.Delay(100);
            return new SearchResult
            {
                SubTaskId = subtask.Id,
                Success = true,
                Commands = new List<CommandMatch>()
            };
        };

        // Act
        var startTime = DateTime.UtcNow;
        var results = await executor.ExecuteAgentsAsync(subtasks, agentFactory);
        var endTime = DateTime.UtcNow;

        // Assert
        Assert.Equal(3, results.Count);
        Assert.All(results, r => Assert.True(r.Success));
        
        // Verify parallel execution (should take ~100ms, not 300ms)
        var totalTime = (endTime - startTime).TotalMilliseconds;
        Assert.True(totalTime < 200, $"Expected parallel execution (~100ms), but took {totalTime}ms");
    }

    [Fact]
    public void ParallelAgentExecutor_HighFailureRate_ShouldAbort()
    {
        // Arrange
        var executor = new ParallelAgentExecutor(_logger);
        var results = new List<SearchResult>
        {
            new SearchResult { SubTaskId = 1, Success = false },
            new SearchResult { SubTaskId = 2, Success = false },
            new SearchResult { SubTaskId = 3, Success = false },
            new SearchResult { SubTaskId = 4, Success = true }
        };

        // Act
        var shouldAbort = executor.ShouldAbortDueToFailures(results);

        // Assert
        Assert.True(shouldAbort); // 75% failure rate > 50% threshold
    }

    [Fact]
    public void ParallelAgentExecutor_LowFailureRate_ShouldContinue()
    {
        // Arrange
        var executor = new ParallelAgentExecutor(_logger);
        var results = new List<SearchResult>
        {
            new SearchResult { SubTaskId = 1, Success = true },
            new SearchResult { SubTaskId = 2, Success = true },
            new SearchResult { SubTaskId = 3, Success = false },
            new SearchResult { SubTaskId = 4, Success = true }
        };

        // Act
        var shouldAbort = executor.ShouldAbortDueToFailures(results);

        // Assert
        Assert.False(shouldAbort); // 25% failure rate < 50% threshold
    }
}
