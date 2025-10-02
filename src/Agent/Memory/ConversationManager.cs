using Dapper;
using Microsoft.Data.Sqlite;
using Serilog;
using WorkflowPlus.AIAgent.Core.Interfaces;
using WorkflowPlus.AIAgent.Core.Models;

namespace WorkflowPlus.AIAgent.Memory;

/// <summary>
/// Manages conversation persistence using SQLite.
/// </summary>
public class ConversationManager : IConversationManager
{
    private readonly string _dbPath;
    private readonly ILogger _logger;

    public ConversationManager(string dbPath, ILogger logger)
    {
        _dbPath = dbPath;
        _logger = logger;
        InitializeDatabaseAsync().Wait();
    }

    private async Task InitializeDatabaseAsync()
    {
        using var connection = new SqliteConnection($"Data Source={_dbPath}");
        await connection.OpenAsync();

        // Create tables if they don't exist
        await connection.ExecuteAsync(@"
            CREATE TABLE IF NOT EXISTS conversations (
                id TEXT PRIMARY KEY,
                user_id TEXT NOT NULL,
                title TEXT,
                folder_id TEXT,
                created_at TEXT NOT NULL,
                last_modified TEXT NOT NULL,
                total_tokens INTEGER DEFAULT 0,
                total_cost REAL DEFAULT 0.0
            );

            CREATE TABLE IF NOT EXISTS messages (
                id TEXT PRIMARY KEY,
                conversation_id TEXT NOT NULL,
                role TEXT NOT NULL,
                content TEXT,
                reasoning_tokens INTEGER,
                timestamp TEXT NOT NULL,
                FOREIGN KEY (conversation_id) REFERENCES conversations(id) ON DELETE CASCADE
            );

            CREATE TABLE IF NOT EXISTS tool_calls (
                id TEXT PRIMARY KEY,
                message_id TEXT NOT NULL,
                tool_name TEXT NOT NULL,
                input_json TEXT,
                output_json TEXT,
                duration_ms INTEGER,
                timestamp TEXT NOT NULL,
                FOREIGN KEY (message_id) REFERENCES messages(id) ON DELETE CASCADE
            );

            CREATE TABLE IF NOT EXISTS folders (
                id TEXT PRIMARY KEY,
                name TEXT NOT NULL,
                parent_id TEXT,
                created_at TEXT NOT NULL,
                FOREIGN KEY (parent_id) REFERENCES folders(id) ON DELETE CASCADE
            );

            CREATE INDEX IF NOT EXISTS idx_conversations_user ON conversations(user_id);
            CREATE INDEX IF NOT EXISTS idx_messages_conversation ON messages(conversation_id);
            CREATE INDEX IF NOT EXISTS idx_tool_calls_message ON tool_calls(message_id);
        ");

        _logger.Information("Database initialized at {Path}", _dbPath);
    }

    public async Task<string> CreateConversationAsync(string userId, string title)
    {
        var conversationId = Guid.NewGuid().ToString();
        var now = DateTime.UtcNow.ToString("O");

        using var connection = new SqliteConnection($"Data Source={_dbPath}");
        await connection.OpenAsync();

        await connection.ExecuteAsync(@"
            INSERT INTO conversations (id, user_id, title, created_at, last_modified)
            VALUES (@Id, @UserId, @Title, @CreatedAt, @LastModified)",
            new
            {
                Id = conversationId,
                UserId = userId,
                Title = title,
                CreatedAt = now,
                LastModified = now
            });

        _logger.Information("Created conversation {ConversationId} for user {UserId}", conversationId, userId);
        return conversationId;
    }

    public async Task SaveMessageAsync(ConversationMessage message)
    {
        using var connection = new SqliteConnection($"Data Source={_dbPath}");
        await connection.OpenAsync();

        await connection.ExecuteAsync(@"
            INSERT INTO messages (id, conversation_id, role, content, reasoning_tokens, timestamp)
            VALUES (@Id, @ConversationId, @Role, @Content, @ReasoningTokens, @Timestamp)",
            new
            {
                message.Id,
                message.ConversationId,
                message.Role,
                message.Content,
                message.ReasoningTokens,
                Timestamp = message.Timestamp.ToString("O")
            });

        // Update conversation last_modified
        await connection.ExecuteAsync(@"
            UPDATE conversations 
            SET last_modified = @LastModified 
            WHERE id = @ConversationId",
            new
            {
                ConversationId = message.ConversationId,
                LastModified = DateTime.UtcNow.ToString("O")
            });

        _logger.Debug("Saved message {MessageId} to conversation {ConversationId}", 
            message.Id, message.ConversationId);
    }

    public async Task<List<ConversationMessage>> GetConversationHistoryAsync(string conversationId)
    {
        using var connection = new SqliteConnection($"Data Source={_dbPath}");
        await connection.OpenAsync();

        var messages = await connection.QueryAsync<ConversationMessage>(@"
            SELECT id, conversation_id as ConversationId, role, content, 
                   reasoning_tokens as ReasoningTokens, timestamp
            FROM messages
            WHERE conversation_id = @ConversationId
            ORDER BY timestamp ASC",
            new { ConversationId = conversationId });

        return messages.Select(m =>
        {
            m.Timestamp = DateTime.Parse(m.Timestamp.ToString()!);
            return m;
        }).ToList();
    }

    public async Task<List<string>> SearchConversationsAsync(string userId, string searchTerm)
    {
        using var connection = new SqliteConnection($"Data Source={_dbPath}");
        await connection.OpenAsync();

        var conversationIds = await connection.QueryAsync<string>(@"
            SELECT DISTINCT c.id
            FROM conversations c
            LEFT JOIN messages m ON m.conversation_id = c.id
            WHERE c.user_id = @UserId
              AND (c.title LIKE @Search OR m.content LIKE @Search)
            ORDER BY c.last_modified DESC
            LIMIT 50",
            new
            {
                UserId = userId,
                Search = $"%{searchTerm}%"
            });

        return conversationIds.ToList();
    }

    public async Task DeleteConversationAsync(string conversationId)
    {
        using var connection = new SqliteConnection($"Data Source={_dbPath}");
        await connection.OpenAsync();

        await connection.ExecuteAsync(@"
            DELETE FROM conversations WHERE id = @ConversationId",
            new { ConversationId = conversationId });

        _logger.Information("Deleted conversation {ConversationId}", conversationId);
    }

    public async Task UpdateConversationCostAsync(string conversationId, int tokens, decimal cost)
    {
        using var connection = new SqliteConnection($"Data Source={_dbPath}");
        await connection.OpenAsync();

        await connection.ExecuteAsync(@"
            UPDATE conversations 
            SET total_tokens = total_tokens + @Tokens,
                total_cost = total_cost + @Cost
            WHERE id = @ConversationId",
            new
            {
                ConversationId = conversationId,
                Tokens = tokens,
                Cost = cost
            });
    }
}
