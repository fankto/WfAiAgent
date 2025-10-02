using Dapper;
using Microsoft.Data.Sqlite;
using Serilog;

namespace WorkflowPlus.AIAgent.Observability;

/// <summary>
/// Tracks token usage and costs for monitoring and billing.
/// </summary>
public class CostTracker
{
    private readonly string _dbPath;
    private readonly ILogger _logger;

    public CostTracker(string dbPath, ILogger logger)
    {
        _dbPath = dbPath;
        _logger = logger;
    }

    public async Task<CostSummary> GetUserCostSummaryAsync(string userId, DateTime startDate, DateTime endDate)
    {
        using var connection = new SqliteConnection($"Data Source={_dbPath}");
        await connection.OpenAsync();

        var summary = await connection.QuerySingleOrDefaultAsync<CostSummary>(@"
            SELECT
                COUNT(*) as TotalQueries,
                SUM(total_tokens) as TotalTokens,
                SUM(total_cost) as TotalCostUSD,
                AVG(total_cost) as AvgCostPerQuery,
                MAX(total_cost) as MaxCostPerQuery
            FROM conversations
            WHERE user_id = @UserId
              AND created_at BETWEEN @StartDate AND @EndDate",
            new
            {
                UserId = userId,
                StartDate = startDate.ToString("O"),
                EndDate = endDate.ToString("O")
            });

        return summary ?? new CostSummary();
    }

    public async Task<List<DailyCost>> GetDailyCostsAsync(string userId, int days = 30)
    {
        using var connection = new SqliteConnection($"Data Source={_dbPath}");
        await connection.OpenAsync();

        var costs = await connection.QueryAsync<DailyCost>(@"
            SELECT
                DATE(created_at) as Date,
                COUNT(*) as QueryCount,
                SUM(total_tokens) as TotalTokens,
                SUM(total_cost) as TotalCost
            FROM conversations
            WHERE user_id = @UserId
              AND created_at >= datetime('now', '-' || @Days || ' days')
            GROUP BY DATE(created_at)
            ORDER BY Date DESC",
            new { UserId = userId, Days = days });

        return costs.ToList();
    }

    public async Task LogQueryCostAsync(string conversationId, string userId, int tokens, decimal cost, string model)
    {
        _logger.Information(
            "Query cost logged - User: {UserId}, Conversation: {ConversationId}, Tokens: {Tokens}, Cost: ${Cost:F4}, Model: {Model}",
            userId, conversationId, tokens, cost, model);

        // Cost is already tracked in conversations table by ConversationManager
        // This method provides additional logging and monitoring
    }

    public async Task<bool> CheckCostLimitAsync(string userId, decimal maxDailyCost)
    {
        var today = DateTime.UtcNow.Date;
        var tomorrow = today.AddDays(1);

        var summary = await GetUserCostSummaryAsync(userId, today, tomorrow);

        if (summary.TotalCostUSD >= maxDailyCost)
        {
            _logger.Warning(
                "User {UserId} has exceeded daily cost limit. Current: ${Current:F2}, Limit: ${Limit:F2}",
                userId, summary.TotalCostUSD, maxDailyCost);
            return false;
        }

        return true;
    }
}

public class CostSummary
{
    public int TotalQueries { get; set; }
    public int TotalTokens { get; set; }
    public decimal TotalCostUSD { get; set; }
    public decimal AvgCostPerQuery { get; set; }
    public decimal MaxCostPerQuery { get; set; }
}

public class DailyCost
{
    public string Date { get; set; } = string.Empty;
    public int QueryCount { get; set; }
    public int TotalTokens { get; set; }
    public decimal TotalCost { get; set; }
}
