using FluentAssertions;
using WorkflowPlus.AIAgent.Tools;
using Xunit;

namespace WorkflowPlus.AIAgent.Tests.Unit;

public class SearchKnowledgeToolTests
{
    [Fact]
    public async Task SearchCommandsAsync_WithValidQuery_ReturnsFormattedResults()
    {
        // Arrange
        var tool = new SearchKnowledgeTool();
        var query = "find customer by name";

        // Act
        var result = await tool.SearchCommandsAsync(query, maxResults: 5);

        // Assert
        result.Should().NotBeNullOrEmpty();
        // Note: This test requires AiSearch service to be running
        // In a real test, we would mock the HttpClient
    }

    [Fact]
    public async Task SearchCommandsAsync_WithEmptyQuery_HandlesGracefully()
    {
        // Arrange
        var tool = new SearchKnowledgeTool();
        var query = "";

        // Act
        var result = await tool.SearchCommandsAsync(query);

        // Assert
        result.Should().NotBeNull();
    }
}
