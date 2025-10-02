using FluentAssertions;
using Serilog;
using WorkflowPlus.AIAgent.Core.Models;
using WorkflowPlus.AIAgent.Memory;
using Xunit;

namespace WorkflowPlus.AIAgent.Tests.Unit;

public class ConversationManagerTests : IDisposable
{
    private readonly string _testDbPath;
    private readonly ConversationManager _manager;

    public ConversationManagerTests()
    {
        _testDbPath = Path.Combine(Path.GetTempPath(), $"test_{Guid.NewGuid()}.db");
        _manager = new ConversationManager(_testDbPath, Log.Logger);
    }

    [Fact]
    public async Task CreateConversationAsync_CreatesNewConversation()
    {
        // Arrange
        var userId = "test-user";
        var title = "Test Conversation";

        // Act
        var conversationId = await _manager.CreateConversationAsync(userId, title);

        // Assert
        conversationId.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task SaveMessageAsync_PersistsMessage()
    {
        // Arrange
        var conversationId = await _manager.CreateConversationAsync("test-user", "Test");
        var message = new ConversationMessage
        {
            ConversationId = conversationId,
            Role = "user",
            Content = "Hello, agent!"
        };

        // Act
        await _manager.SaveMessageAsync(message);
        var history = await _manager.GetConversationHistoryAsync(conversationId);

        // Assert
        history.Should().HaveCount(1);
        history[0].Content.Should().Be("Hello, agent!");
    }

    [Fact]
    public async Task GetConversationHistoryAsync_ReturnsMessagesInOrder()
    {
        // Arrange
        var conversationId = await _manager.CreateConversationAsync("test-user", "Test");
        
        await _manager.SaveMessageAsync(new ConversationMessage
        {
            ConversationId = conversationId,
            Role = "user",
            Content = "First message"
        });

        await Task.Delay(10); // Ensure different timestamps

        await _manager.SaveMessageAsync(new ConversationMessage
        {
            ConversationId = conversationId,
            Role = "assistant",
            Content = "Second message"
        });

        // Act
        var history = await _manager.GetConversationHistoryAsync(conversationId);

        // Assert
        history.Should().HaveCount(2);
        history[0].Content.Should().Be("First message");
        history[1].Content.Should().Be("Second message");
    }

    [Fact]
    public async Task SearchConversationsAsync_FindsMatchingConversations()
    {
        // Arrange
        var userId = "test-user";
        var conv1 = await _manager.CreateConversationAsync(userId, "Customer Management");
        var conv2 = await _manager.CreateConversationAsync(userId, "Invoice Processing");

        await _manager.SaveMessageAsync(new ConversationMessage
        {
            ConversationId = conv1,
            Role = "user",
            Content = "How do I find a customer?"
        });

        // Act
        var results = await _manager.SearchConversationsAsync(userId, "customer");

        // Assert
        results.Should().Contain(conv1);
    }

    [Fact]
    public async Task DeleteConversationAsync_RemovesConversation()
    {
        // Arrange
        var conversationId = await _manager.CreateConversationAsync("test-user", "Test");
        await _manager.SaveMessageAsync(new ConversationMessage
        {
            ConversationId = conversationId,
            Role = "user",
            Content = "Test message"
        });

        // Act
        await _manager.DeleteConversationAsync(conversationId);
        var history = await _manager.GetConversationHistoryAsync(conversationId);

        // Assert
        history.Should().BeEmpty();
    }

    public void Dispose()
    {
        if (File.Exists(_testDbPath))
        {
            File.Delete(_testDbPath);
        }
    }
}
