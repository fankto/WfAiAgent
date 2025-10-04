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

    [Fact]
    public async Task TaskDecomposer_SimpleRequest_ReturnsOneSubtask()
    {
        // Arrange
        var decomposer = new TaskDecomposer(_mockChatService.Object, _logger);
        
        // Mock LLM response for simple request
        var mockResponse = new Mock<ChatMessageContent>();
        mockResponse.Setup(r => r.Content).Returns(@"
        {
            ""subtasks"": [
                {""id"": 1, ""description"": ""Create an array"", ""depends_on"": []}
            ]
        }");
        
        _mockChatService
            .Setup(s => s.GetChatMessageContentAsync(
                It.IsAny<string>(),
                It.IsAny<PromptExecutionSettings>(),
                It.IsAny<Kernel>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(mockResponse.Object);

        // Act
        var result = await decomposer.DecomposeAsync("Create an array");

        // Assert
        Assert.True(result.Success);
        Assert.Single(result.SubTasks);
        Assert.Equal(1, result.SubTasks[0].Id);
        Assert.Equal("Create an array", result.SubTasks[0].Description);
        Assert.Empty(result.SubTasks[0].DependsOn);
    }

    [Fact]
    public async Task TaskDecomposer_ComplexRequest_ReturnsMultipleSubtasks()
    {
        // Arrange
        var decomposer = new TaskDecomposer(_mockChatService.Object, _logger);
        
        // Mock LLM response for complex request
        var mockResponse = new Mock<ChatMessageContent>();
        mockResponse.Setup(r => r.Content).Returns(@"
        {
            ""subtasks"": [
                {""id"": 1, ""description"": ""Create and populate array"", ""depends_on"": []},
                {""id"": 2, ""description"": ""Sort array"", ""depends_on"": [1]},
                {""id"": 3, ""description"": ""Save to file"", ""depends_on"": [2]}
            ]
        }");
        
        _mockChatService
            .Setup(s => s.GetChatMessageContentAsync(
                It.IsAny<string>(),
                It.IsAny<PromptExecutionSettings>(),
                It.IsAny<Kernel>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(mockResponse.Object);

        // Act
        var result = await decomposer.DecomposeAsync("Create list, sort it, save to file");

        // Assert
        Assert.True(result.Success);
        Assert.Equal(3, result.SubTasks.Count);
        
        // Check dependencies
        Assert.Empty(result.SubTasks[0].DependsOn);
        Assert.Single(result.SubTasks[1].DependsOn);
        Assert.Contains(1, result.SubTasks[1].DependsOn);
        Assert.Single(result.SubTasks[2].DependsOn);
        Assert.Contains(2, result.SubTasks[2].DependsOn);
    }

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
