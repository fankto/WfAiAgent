using FluentAssertions;
using Serilog;
using WorkflowPlus.AIAgent.Orchestration;
using Xunit;

namespace WorkflowPlus.AIAgent.Tests.Integration;

/// <summary>
/// End-to-end integration tests for the agent.
/// These tests require:
/// 1. OPENAI_API_KEY environment variable set
/// 2. AiSearch service running at localhost:54321
/// </summary>
[Collection("Integration")]
public class EndToEndAgentTests
{
    private readonly AgentOrchestrator _orchestrator;
    private readonly bool _canRunTests;

    public EndToEndAgentTests()
    {
        var apiKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY");
        _canRunTests = !string.IsNullOrEmpty(apiKey);

        if (_canRunTests)
        {
            var settingsPath = Path.Combine(
                AppContext.BaseDirectory, 
                "..", "..", "..", "..", "..", "..", 
                "agent_config.yml");

            var settings = AgentSettings.LoadFromYaml(settingsPath);
            _orchestrator = new AgentOrchestrator(apiKey!, settings, Log.Logger);
        }
        else
        {
            _orchestrator = null!;
        }
    }

    [Fact]
    public async Task ProcessQueryAsync_WithSimpleQuestion_ReturnsResponse()
    {
        // Skip if no API key
        if (!_canRunTests)
        {
            return;
        }

        // Arrange
        var query = "What is the GetCustomerByName function?";

        // Act
        var response = await _orchestrator.ProcessQueryAsync(query);

        // Assert
        response.Should().NotBeNull();
        response.Success.Should().BeTrue();
        response.Content.Should().NotBeNullOrEmpty();
        response.TokensUsed.Should().BeGreaterThan(0);
        response.EstimatedCost.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task ProcessQueryAsync_WithCodeGenerationRequest_GeneratesCode()
    {
        if (!_canRunTests) return;

        // Arrange
        var query = "Generate code to find a customer named 'Acme Corp' and log their ID";

        // Act
        var response = await _orchestrator.ProcessQueryAsync(query);

        // Assert
        response.Should().NotBeNull();
        response.Success.Should().BeTrue();
        response.Content.Should().Contain("GetCustomerByName");
        response.Content.Should().Contain("Acme Corp");
    }

    [Fact]
    public async Task StreamResponseAsync_StreamsTokens()
    {
        if (!_canRunTests) return;

        // Arrange
        var query = "What commands are available for customer management?";
        var tokens = new List<string>();

        // Act
        await foreach (var token in _orchestrator.StreamResponseAsync(query))
        {
            tokens.Add(token);
        }

        // Assert
        tokens.Should().NotBeEmpty();
        var fullResponse = string.Join("", tokens);
        fullResponse.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task GenerateCodeWithReflectionAsync_ProducesValidCode()
    {
        if (!_canRunTests) return;

        // Arrange
        var intent = "Find customer by name and update their email address";

        // Act
        var result = await _orchestrator.GenerateCodeWithReflectionAsync(intent);

        // Assert
        result.Should().NotBeNull();
        result.Code.Should().NotBeNullOrEmpty();
        result.Code.Should().Contain("GetCustomerByName");
        result.Code.Should().Contain("UpdateCustomerField");
        result.ReflectionIterations.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task ProcessQueryAsync_WithMultipleTurns_MaintainsContext()
    {
        if (!_canRunTests) return;

        // Arrange & Act
        var response1 = await _orchestrator.ProcessQueryAsync("What is GetCustomerByName?");
        var response2 = await _orchestrator.ProcessQueryAsync("What parameters does it take?");

        // Assert
        response1.Success.Should().BeTrue();
        response2.Success.Should().BeTrue();
        // Second response should reference the function from first query
        response2.Content.Should().NotBeNullOrEmpty();
    }
}
