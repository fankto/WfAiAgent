using WorkflowPlus.AIAgent.Core.Models;

namespace WorkflowPlus.AIAgent.Core.Interfaces;

/// <summary>
/// Interface for managing conversation persistence.
/// </summary>
public interface IConversationManager
{
    Task<string> CreateConversationAsync(string userId, string title);
    Task SaveMessageAsync(ConversationMessage message);
    Task<List<ConversationMessage>> GetConversationHistoryAsync(string conversationId);
    Task<List<string>> SearchConversationsAsync(string userId, string searchTerm);
    Task DeleteConversationAsync(string conversationId);
}
