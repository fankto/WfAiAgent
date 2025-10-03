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
    private const float MinAcceptableScore = 0.6f;
    
    // Redundant terms to remove from queries
    private static readonly string[] RedundantTerms = new[]
    {
        "in workflow+", "in workflowplus", "using workflow+", "using workflowplus",
        "with workflow+", "with workflowplus", "workflow+ command", "workflowplus command"
    };

    // German-English synonym pairs for better search
    private static readonly Dictionary<string, string> Synonyms = new()
    {
        { "liste", "list" },
        { "array", "list" },
        { "sortieren", "sort" },
        { "datenbank", "database" },
        { "tabelle", "table" },
        { "feld", "field" },
        { "datei", "file" },
        { "erstellen", "create" },
        { "löschen", "delete" },
        { "aktualisieren", "update" }
    };

    public SearchKnowledgeTool()
    {
        _httpClient = new HttpClient
        {
            BaseAddress = new Uri(AiSearchBaseUrl),
            Timeout = TimeSpan.FromSeconds(10) // Increased for refinement attempts
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

        // Use search with refinement
        var searchResult = await SearchWithRefinementAsync(query, maxResults);
        
        if (searchResult.Results.Count == 0)
        {
            return $"No commands found after trying multiple search strategies. Attempted queries: {string.Join(", ", searchResult.QueriesAttempted)}";
        }

        // Format results for LLM consumption
        var sb = new StringBuilder();
        
        if (searchResult.RequiredRefinement)
        {
            sb.AppendLine($"Note: Initial search was refined. Tried: {string.Join(", ", searchResult.QueriesAttempted)}\n");
        }
        
        sb.AppendLine($"Found {searchResult.Results.Count} relevant commands:\n");

        foreach (var result in searchResult.Results)
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

        _logger.Information("Returning {Count} results with average score {AvgScore:F2}", 
            searchResult.Results.Count, searchResult.AverageScore);
        
        return sb.ToString();
    }

    /// <summary>
    /// Search with automatic query refinement if initial results are poor
    /// </summary>
    public async Task<SearchResult> SearchWithRefinementAsync(
        string query,
        int maxResults = 5,
        int maxAttempts = 3)
    {
        var queriesAttempted = new List<string>();
        var bestResult = new SearchResult { Results = new List<CommandMatch>() };
        
        // Attempt 1: Formulated query (remove redundant terms)
        var formulatedQuery = FormulateQuery(query);
        queriesAttempted.Add(formulatedQuery);
        
        var results = await ExecuteSearchAsync(formulatedQuery, maxResults);
        if (results.Any() && results.Average(r => r.Score) >= MinAcceptableScore)
        {
            return new SearchResult
            {
                Results = results,
                QueriesAttempted = queriesAttempted,
                RequiredRefinement = false,
                AverageScore = results.Average(r => r.Score)
            };
        }
        
        bestResult.Results = results;
        bestResult.AverageScore = results.Any() ? results.Average(r => r.Score) : 0;
        
        _logger.Information("Initial search score {Score:F2} below threshold, refining...", bestResult.AverageScore);
        
        // Attempt 2: Break down complex queries
        if (maxAttempts > 1)
        {
            var brokenDownQueries = BreakDownQuery(formulatedQuery);
            foreach (var subQuery in brokenDownQueries.Take(maxAttempts - 1))
            {
                queriesAttempted.Add(subQuery);
                results = await ExecuteSearchAsync(subQuery, maxResults);
                
                if (results.Any())
                {
                    var avgScore = results.Average(r => r.Score);
                    if (avgScore > bestResult.AverageScore)
                    {
                        bestResult.Results = results;
                        bestResult.AverageScore = avgScore;
                    }
                    
                    if (avgScore >= MinAcceptableScore)
                    {
                        _logger.Information("Refinement successful with query: {Query}", subQuery);
                        break;
                    }
                }
            }
        }
        
        bestResult.QueriesAttempted = queriesAttempted;
        bestResult.RequiredRefinement = queriesAttempted.Count > 1;
        
        return bestResult;
    }

    /// <summary>
    /// Formulate query by removing redundant terms
    /// </summary>
    public string FormulateQuery(string userInput)
    {
        var query = userInput.ToLowerInvariant();
        
        // Remove redundant terms
        foreach (var term in RedundantTerms)
        {
            query = Regex.Replace(query, Regex.Escape(term), "", RegexOptions.IgnoreCase);
        }
        
        // Clean up extra whitespace
        query = Regex.Replace(query, @"\s+", " ").Trim();
        
        _logger.Debug("Formulated query: '{Original}' -> '{Formulated}'", userInput, query);
        
        return query;
    }

    /// <summary>
    /// Break down complex queries into simpler sub-queries
    /// </summary>
    private List<string> BreakDownQuery(string query)
    {
        var subQueries = new List<string>();
        
        // Split on common conjunctions
        var parts = Regex.Split(query, @"\s+and\s+|\s+,\s+", RegexOptions.IgnoreCase);
        
        if (parts.Length > 1)
        {
            subQueries.AddRange(parts.Select(p => p.Trim()).Where(p => !string.IsNullOrWhiteSpace(p)));
        }
        
        // Extract key action verbs
        var actionVerbs = new[] { "create", "sort", "add", "delete", "update", "get", "set", "find", "search" };
        foreach (var verb in actionVerbs)
        {
            if (query.Contains(verb, StringComparison.OrdinalIgnoreCase))
            {
                // Extract the verb and the next few words
                var match = Regex.Match(query, $@"\b{verb}\b\s+\w+(?:\s+\w+)?", RegexOptions.IgnoreCase);
                if (match.Success)
                {
                    subQueries.Add(match.Value.Trim());
                }
            }
        }
        
        return subQueries.Distinct().ToList();
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
/// Result of a search with refinement
/// </summary>
public class SearchResult
{
    public List<CommandMatch> Results { get; set; } = new();
    public List<string> QueriesAttempted { get; set; } = new();
    public bool RequiredRefinement { get; set; }
    public float AverageScore { get; set; }
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
