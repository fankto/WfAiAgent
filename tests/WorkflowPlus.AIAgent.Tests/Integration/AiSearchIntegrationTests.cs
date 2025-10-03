using FluentAssertions;
using WorkflowPlus.AIAgent.Tools;
using Xunit;

namespace WorkflowPlus.AIAgent.Tests.Integration;

/// <summary>
/// Integration tests for AiSearch service connectivity.
/// Requires AiSearch service running at localhost:54321
/// </summary>
[Collection("Integration")]
public class AiSearchIntegrationTests
{
    private readonly SearchKnowledgeTool _tool;
    private readonly bool _canRunTests;

    public AiSearchIntegrationTests()
    {
        _tool = new SearchKnowledgeTool();
        
        // Check if AiSearch is available
        _canRunTests = CheckAiSearchAvailability().Result;
    }

    private async Task<bool> CheckAiSearchAvailability()
    {
        try
        {
            using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(2) };
            var response = await client.GetAsync("http://localhost:54321/health");
            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }

    [Fact]
    public async Task SearchCommandsAsync_WithDataTableQuery_ReturnsDataTableCommands()
    {
        if (!_canRunTests)
        {
            // Skip test if AiSearch not available
            return;
        }

        // Arrange
        var query = "DataTable";

        // Act
        var result = await _tool.SearchCommandsAsync(query, maxResults: 5);

        // Assert
        result.Should().NotBeNullOrEmpty();
        result.Should().Contain("DataTable");
    }

    [Fact]
    public async Task SearchCommandsAsync_WithXmlQuery_ReturnsXmlCommands()
    {
        if (!_canRunTests) return;

        // Arrange
        var query = "XML";

        // Act
        var result = await _tool.SearchCommandsAsync(query, maxResults: 5);

        // Assert
        result.Should().NotBeNullOrEmpty();
        result.Should().ContainAny("Xml", "XML");
    }

    [Fact]
    public async Task SearchCommandsAsync_WithDatabaseQuery_ReturnsDatabaseCommands()
    {
        if (!_canRunTests) return;

        // Arrange
        var query = "database operations";

        // Act
        var result = await _tool.SearchCommandsAsync(query, maxResults: 10);

        // Assert
        result.Should().NotBeNullOrEmpty();
        result.Should().ContainAny("SQL", "Database", "Query", "Connection");
    }

    [Fact]
    public async Task SearchCommandsAsync_WithNonExistentQuery_HandlesGracefully()
    {
        if (!_canRunTests) return;

        // Arrange
        var query = "xyzabc123nonexistent";

        // Act
        var result = await _tool.SearchCommandsAsync(query, maxResults: 5);

        // Assert
        result.Should().NotBeNull();
        // Should either return "No commands found" or empty results
    }

    [Fact]
    public async Task SearchCommandsAsync_WithMaxResults_RespectsLimit()
    {
        if (!_canRunTests) return;

        // Arrange
        var query = "customer";
        var maxResults = 3;

        // Act
        var result = await _tool.SearchCommandsAsync(query, maxResults);

        // Assert
        result.Should().NotBeNullOrEmpty();
        // Count occurrences of "##" which indicates command headers
        var commandCount = result.Split("##").Length - 1;
        commandCount.Should().BeLessOrEqualTo(maxResults);
    }
}
