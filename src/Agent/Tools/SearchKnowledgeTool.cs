using System.ComponentModel;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.SemanticKernel;
using Serilog;

namespace WorkflowPlus.AIAgent.Tools;

/// <summary>
/// Tool for searching Workflow+ documentation using the AiSearch service.
/// Enhanced with query refinement and intelligent search strategies.
/// </summary>
public class SearchKnowledgeTool
{
    private readonly HttpClient _httpClient;
    private readonly ILogger _logger;
    private const string AiSearchBaseUrl = "http://localhost:54321";

    public SearchKnowledgeTool()
    {
        _httpClient = new HttpClient
        {
            BaseAddress = new Uri(AiSearchBaseUrl),
            Timeout = TimeSpan.FromSeconds(10)
        };
        _logger = Log.ForContext<SearchKnowledgeTool>();
    }

    [KernelFunction("search_commands")]
    [Description("Search documentation for commands and functions. Pass clean, minimal search queries (e.g., 'create array', 'sort list', 'send email').")]
    public async Task<string> SearchCommandsAsync(
        [Description("Search query - keep it simple and technical (e.g., 'create array', 'sort list')")] string query,
        [Description("Maximum number of results to return (default 5)")] int maxResults = 5)
    {
        _logger.Information("Searching documentation for: {Query}", query);

        // Just search directly - let the LLM formulate good queries via prompts
        var results = await ExecuteSearchAsync(query, maxResults);
        
        if (results.Count == 0)
        {
            return $"No commands found for query: {query}";
        }

        // Format results for LLM consumption
        var sb = new StringBuilder();
        sb.AppendLine($"Found {results.Count} relevant commands:\n");

        foreach (var result in results)
        {
            sb.AppendLine($"## {result.Name}");
            sb.AppendLine($"**Source:** [{result.Name}]({result.SourceFile})");
            sb.AppendLine($"**License Tier:** {result.LicenseTier}");
            
            if (!string.IsNullOrEmpty(result.Category))
                sb.AppendLine($"**Category:** {result.Category}");
            
            if (!string.IsNullOrEmpty(result.PluginName))
                sb.AppendLine($"**Plugin:** {result.PluginName}");
            
            sb.AppendLine($"**Description:** {result.Description}");
            
            if (!string.IsNullOrEmpty(result.Syntax))
                sb.AppendLine($"**Syntax:** `{result.Syntax}`");
            
            if (!string.IsNullOrEmpty(result.Parameters))
                sb.AppendLine($"**Parameters:** {result.Parameters}");
            
            sb.AppendLine($"**Relevance Score:** {result.Score:F2}");
            sb.AppendLine();
        }

        var avgScore = results.Average(r => r.Score);
        _logger.Information("Returning {Count} results with average score {AvgScore:F2}", results.Count, avgScore);
        
        return sb.ToString();
    }



    /// <summary>
    /// Execute a single search query
    /// </summary>
    private async Task<List<CommandMatch>> ExecuteSearchAsync(string query, int maxResults)
    {
        try
        {
            var url = $"/search?query={Uri.EscapeDataString(query)}&pageSize={maxResults}";
            var response = await _httpClient.GetAsync(url);
            
            if (!response.IsSuccessStatusCode)
            {
                _logger.Warning("AiSearch returned status {Status} for query: {Query}", response.StatusCode, query);
                return new List<CommandMatch>();
            }

            var jsonResponse = await response.Content.ReadAsStringAsync();
            var searchResults = JsonSerializer.Deserialize<SearchApiResponse>(jsonResponse, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            if (searchResults == null || searchResults.Results.Count == 0)
            {
                return new List<CommandMatch>();
            }

            return searchResults.Results.Select(r => new CommandMatch
            {
                Name = r.Document.Name,
                Syntax = r.Document.Syntax ?? "",
                Parameters = r.Document.Parameters ?? "",
                Description = r.Document.Description ?? "",
                SourceFile = $"{r.Document.Name}.md",
                LicenseTier = r.Document.LicenseTier ?? "Basic",
                Category = r.Document.Category ?? "",
                PluginName = r.Document.PluginName ?? "",
                Score = r.Score
            }).ToList();
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Error executing search for query: {Query}", query);
            return new List<CommandMatch>();
        }
    }

    // DTOs matching AiSearch API response
    private class SearchApiResponse
    {
        public List<SearchResultItem> Results { get; set; } = new();
        public PaginationInfo Pagination { get; set; } = new();
    }

    private class SearchResultItem
    {
        public CommandDocument Document { get; set; } = new();
        public float Score { get; set; }
    }

    private class CommandDocument
    {
        public string Name { get; set; } = string.Empty;
        public string? Category { get; set; }
        public string? PluginName { get; set; }
        public string? Description { get; set; }
        public string? Syntax { get; set; }
        public string? Parameters { get; set; }
        public string? LicenseTier { get; set; }
    }

    private class PaginationInfo
    {
        public int TotalResults { get; set; }
        public int TotalPages { get; set; }
        public int CurrentPage { get; set; }
        public int PageSize { get; set; }
    }
}

/// <summary>
/// A matched command with full details
/// </summary>
public class CommandMatch
{
    public string Name { get; set; } = string.Empty;
    public string Syntax { get; set; } = string.Empty;
    public string Parameters { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string SourceFile { get; set; } = string.Empty;
    public string LicenseTier { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string PluginName { get; set; } = string.Empty;
    public float Score { get; set; }
    public string FullDocumentation { get; set; } = string.Empty;
}
