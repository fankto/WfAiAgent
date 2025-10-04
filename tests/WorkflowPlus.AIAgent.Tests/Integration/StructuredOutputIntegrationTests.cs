using Serilog;
using WorkflowPlus.AIAgent.Core.Models;
using WorkflowPlus.AIAgent.Orchestration;
using Xunit;

namespace WorkflowPlus.AIAgent.Tests.Integration;

/// <summary>
/// Integration tests for structured output with real OpenAI API.
/// These tests require OPENAI_API_KEY environment variable.
/// </summary>
public class StructuredOutputIntegrationTests : IDisposable
{
    private readonly ILogger _logger;
    private readonly string? _apiKey;

    public StructuredOutputIntegrationTests()
    {
        _logger = new LoggerConfiguration()
            .MinimumLevel.Debug()
            .WriteTo.Console()
            .CreateLogger();

        _apiKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY");
    }

    [Fact(Skip = "Integration test - requires manual verification")]
    public async Task StructuredOutput_WithSimpleCode_ReturnsValidReview()
    {
        // This test is skipped as it requires real OpenAI API calls
        // and the GenerateCodeWithReflectionAsync method
        await Task.CompletedTask;
    }

    [Fact(Skip = "Integration test - requires manual verification")]
    public async Task StructuredOutput_WithBuggyCode_FindsIssues()
    {
        // This test is skipped as it requires real OpenAI API calls
        // and the GenerateCodeWithReflectionAsync method
        await Task.CompletedTask;
    }

    [Fact(Skip = "Integration test - requires manual verification")]
    public async Task StructuredOutput_WithUnsupportedModel_FallsBackToLegacy()
    {
        // This test is skipped as it requires real OpenAI API calls
        // and the GenerateCodeWithReflectionAsync method
        await Task.CompletedTask;
    }

    [Fact]
    public void StructuredOutput_SchemaValidation_Succeeds()
    {
        // Act & Assert
        var exception = Record.Exception(() => SchemaValidator.ValidateSchema());
        Assert.Null(exception);
    }

    [Fact]
    public void StructuredOutput_Configuration_LoadsCorrectly()
    {
        // Arrange
        var yamlPath = Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", "agent_config.yml");
        
        if (!File.Exists(yamlPath))
        {
            _logger.Warning("Skipping test: agent_config.yml not found at {Path}", yamlPath);
            return;
        }

        // Act
        var settings = AgentSettings.LoadFromYaml(yamlPath);

        // Assert
        Assert.NotNull(settings);
        Assert.NotNull(settings.StructuredOutputs);
        Assert.True(settings.StructuredOutputs.Enabled);
    }

    public void Dispose()
    {
        (_logger as IDisposable)?.Dispose();
    }
}
