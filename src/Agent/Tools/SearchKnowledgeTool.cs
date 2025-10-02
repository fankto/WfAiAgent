using System.ComponentModel;
using System.Text;
using System.Text.Json;
using Microsoft.SemanticKernel;
using Serilog;

namespace WorkflowPlus.AIAgent.Tools;

/// <summary>
/// Tool for searching Workflow+ documentation using the AiSearch service.
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
            Timeout = TimeSpan.FromSeconds(5)
        };
        _logger = Log.ForContext<SearchKnowledgeTool>();
    }

    [KernelFunction("search_commands")]
    [Description("Search Workflow+ documentation for commands, functions, and APIs. Use this when you need to find information about how to accomplish a task.")]
    public async Task<string> SearchCommandsAsync(
        [Description("Natural language search query describing what you want to accomplish")] string query,
        [Description("Maximum number of results to return (default 5)")] int maxResults = 5)
    {
        _logger.Information("Searching documentation for: {Query}", query);

        try
        {
            var url = $"/search?query={Uri.EscapeDataString(query)}&pageSize={maxResults}";
            var response = await _httpClient.GetAsync(url);
            
            if (!response.IsSuccessStatusCode)
            {
                _logger.Warning("AiSearch returned status {Status}", response.StatusCode);
                return "Unable to search documentation at this time. Please try again.";
            }

            var jsonResponse = await response.Content.ReadAsStringAsync();
            var searchResults = JsonSerializer.Deserialize<SearchApiResponse>(jsonResponse, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            if (searchResults == null || searchResults.Results.Count == 0)
            {
                return $"No commands found for query: {query}";
            }

            // Format results for LLM consumption
            var sb = new StringBuilder();
            sb.AppendLine($"Found {searchResults.Pagination.TotalResults} commands (showing top {searchResults.Results.Count}):\n");

            foreach (var result in searchResults.Results)
            {
                var doc = result.Document;
                sb.AppendLine($"## {doc.Name}");
                sb.AppendLine($"**License Tier:** {doc.LicenseTier}");
                
                if (!string.IsNullOrEmpty(doc.Category))
                    sb.AppendLine($"**Category:** {doc.Category}");
                
                if (!string.IsNullOrEmpty(doc.PluginName))
                    sb.AppendLine($"**Plugin:** {doc.PluginName}");
                
                sb.AppendLine($"**Description:** {doc.Description}");
                
                if (!string.IsNullOrEmpty(doc.Syntax))
                    sb.AppendLine($"**Syntax:** `{doc.Syntax}`");
                
                if (!string.IsNullOrEmpty(doc.Parameters))
                    sb.AppendLine($"**Parameters:** {doc.Parameters}");
                
                sb.AppendLine($"**Relevance Score:** {result.Score:F2}");
                sb.AppendLine();
            }

            _logger.Information("Found {Count} results for query", searchResults.Results.Count);
            return sb.ToString();
        }
        catch (HttpRequestException ex)
        {
            _logger.Error(ex, "HTTP error searching documentation");
            return "The documentation search service is currently unavailable. Please ensure the AiSearch service is running.";
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Error searching documentation");
            return "An error occurred while searching documentation.";
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
