using System.ComponentModel;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Serilog;

namespace WorkflowPlus.AIAgent.Tools;

/// <summary>
/// State-of-the-art search tool with HyDE and query expansion.
/// Uses advanced RAG techniques to improve retrieval quality.
/// </summary>
public class SearchKnowledgeTool
{
    private readonly HttpClient _httpClient;
    private readonly ILogger _logger;
    private readonly QueryEnhancer? _queryEnhancer;
    private readonly IChatCompletionService? _chatService;
    private const string AiSearchBaseUrl = "http://localhost:54321";
    private const int MinCandidatesForAssessment = 4; // Only assess if we have more than this

    public SearchKnowledgeTool(QueryEnhancer? queryEnhancer = null, IChatCompletionService? chatService = null)
    {
        _httpClient = new HttpClient
        {
            BaseAddress = new Uri(AiSearchBaseUrl),
            Timeout = TimeSpan.FromSeconds(15) // Increased for HyDE processing
        };
        _logger = Log.ForContext<SearchKnowledgeTool>();
        _queryEnhancer = queryEnhancer;
        _chatService = chatService;
    }

    [KernelFunction("search_commands")]
    [Description("Search documentation for commands using state-of-the-art RAG techniques. Pass clean queries.")]
    public async Task<string> SearchCommandsAsync(
        [Description("Search query - keep it simple and technical (e.g., 'create array', 'sort list')")] string query,
        [Description("Maximum number of results to return (default 5)")] int maxResults = 5)
    {
        _logger.Information("Searching documentation for: {Query}", query);

        List<CommandMatch> results;

        // STATE-OF-THE-ART: Use HyDE and query expansion if available
        if (_queryEnhancer != null)
        {
            _logger.Information("Using advanced query techniques (HyDE + Expansion)");
            results = await SearchWithAdvancedTechniquesAsync(query, maxResults);
        }
        else
        {
            // Fallback to direct search
            results = await ExecuteSearchAsync(query, maxResults);
        }

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
    /// STATE-OF-THE-ART: Search using HyDE and query expansion with parallel execution.
    /// This combines multiple advanced techniques for superior retrieval quality.
    /// </summary>
    private async Task<List<CommandMatch>> SearchWithAdvancedTechniquesAsync(string query, int maxResults)
    {
        var allResults = new Dictionary<string, CommandMatch>(); // Deduplicate by command name
        var resultScores = new Dictionary<string, List<float>>(); // Track scores for averaging

        try
        {
            // Clean the query
            var cleanedQuery = _queryEnhancer!.CleanQuery(query);

            // Execute all search techniques in parallel
            var searchTasks = new List<Task<(List<CommandMatch> results, float weight, string technique)>>();

            // Technique 1: Search with original cleaned query
            searchTasks.Add(Task.Run(async () =>
            {
                var results = await ExecuteSearchAsync(cleanedQuery, maxResults);
                return (results, 1.0f, "original");
            }));

            // Technique 2: HyDE - Generate hypothetical document and search
            searchTasks.Add(Task.Run(async () =>
            {
                var hypotheticalDoc = await _queryEnhancer.GenerateHypotheticalDocumentAsync(query);
                var results = await ExecuteSearchAsync(hypotheticalDoc, maxResults);
                return (results, 0.8f, "hyde");
            }));

            // Technique 3: Query Expansion - Generate and search multiple variations
            searchTasks.Add(Task.Run(async () =>
            {
                var expandedQueries = await _queryEnhancer.ExpandQueryAsync(cleanedQuery);
                var expansionResults = new List<CommandMatch>();
                
                // Search all expansions in parallel
                var expansionTasks = expandedQueries.Take(2)
                    .Select(eq => ExecuteSearchAsync(eq, maxResults))
                    .ToList();
                
                var allExpansionResults = await Task.WhenAll(expansionTasks);
                foreach (var results in allExpansionResults)
                {
                    expansionResults.AddRange(results);
                }
                
                return (expansionResults, 0.7f, "expansion");
            }));

            // Wait for all search techniques to complete
            var allSearchResults = await Task.WhenAll(searchTasks);

            // Aggregate results from all techniques
            foreach (var (results, weight, technique) in allSearchResults)
            {
                _logger.Debug("Technique '{Technique}' returned {Count} results", technique, results.Count);
                
                foreach (var result in results)
                {
                    if (!allResults.ContainsKey(result.Name))
                    {
                        allResults[result.Name] = result;
                        resultScores[result.Name] = new List<float>();
                    }
                    resultScores[result.Name].Add(result.Score * weight);
                }
            }

            // Aggregate scores: average all scores for each command
            foreach (var name in allResults.Keys.ToList())
            {
                var avgScore = resultScores[name].Average();
                var match = allResults[name];
                match.Score = avgScore;
                allResults[name] = match;
            }

            // Get candidates sorted by score
            var candidates = allResults.Values
                .OrderByDescending(r => r.Score)
                .ToList();

            _logger.Information("Advanced search returned {Count} unique candidates (from {Total} total hits)",
                candidates.Count, resultScores.Sum(kvp => kvp.Value.Count));

            // Apply LLM relevance assessment if we have enough candidates
            if (candidates.Count > MinCandidatesForAssessment && _chatService != null)
            {
                var relevantResults = await AssessRelevanceAsync(query, candidates, maxResults);
                if (relevantResults.Count > 0)
                {
                    return relevantResults;
                }
                _logger.Warning("LLM assessment returned no results, falling back to score-based ranking");
            }

            // Return top results by aggregated score (fallback or when assessment not needed)
            return candidates.Take(maxResults).ToList();
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Advanced search failed, falling back to basic search");
            return await ExecuteSearchAsync(query, maxResults);
        }
    }

    /// <summary>
    /// Use LLM to assess which candidates are truly relevant to the user's query.
    /// This filters out semantically similar but contextually irrelevant results.
    /// </summary>
    private async Task<List<CommandMatch>> AssessRelevanceAsync(string userQuery, List<CommandMatch> candidates, int maxResults)
    {
        _logger.Information("Assessing relevance of {Count} candidates using LLM", candidates.Count);

        try
        {
            // Build candidate summary (name + brief description only, to save tokens)
            var candidateSummary = new StringBuilder();
            for (int i = 0; i < candidates.Count; i++)
            {
                var cmd = candidates[i];
                candidateSummary.AppendLine($"{i + 1}. {cmd.Name} - {TruncateDescription(cmd.Description, 100)}");
            }

            var assessmentPrompt = $@"User's search query: ""{userQuery}""

Found {candidates.Count} candidate commands:
{candidateSummary}

Task: Determine which commands are TRULY relevant for solving the user's query.
- Consider the user's actual intent, not just keyword matches
- A command is relevant if it directly helps accomplish the user's goal
- Exclude commands that are only tangentially related
- Select between 1 and {maxResults} commands

Return ONLY a JSON object with this structure:
{{
  ""relevant_command_names"": [""CommandName1"", ""CommandName2""],
  ""reasoning"": ""Brief explanation of why these commands were selected""
}}";

            var response = await _chatService!.GetChatMessageContentAsync(assessmentPrompt);
            var content = response.Content?.Trim() ?? "";

            // Extract JSON from response (handle markdown code blocks)
            var jsonStart = content.IndexOf('{');
            var jsonEnd = content.LastIndexOf('}');
            if (jsonStart >= 0 && jsonEnd > jsonStart)
            {
                var json = content.Substring(jsonStart, jsonEnd - jsonStart + 1);
                var assessment = JsonSerializer.Deserialize<RelevanceAssessment>(json, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                if (assessment?.RelevantCommandNames != null && assessment.RelevantCommandNames.Count > 0)
                {
                    // Filter candidates to only include assessed relevant commands
                    var relevantResults = candidates
                        .Where(c => assessment.RelevantCommandNames.Contains(c.Name, StringComparer.OrdinalIgnoreCase))
                        .Take(maxResults)
                        .ToList();

                    _logger.Information("LLM selected {Selected} of {Total} candidates as relevant. Reasoning: {Reasoning}",
                        relevantResults.Count, candidates.Count, assessment.Reasoning);

                    return relevantResults;
                }
            }

            _logger.Warning("Failed to parse LLM assessment response");
            return new List<CommandMatch>();
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Error during LLM relevance assessment");
            return new List<CommandMatch>();
        }
    }

    /// <summary>
    /// Truncate description to specified length for token efficiency
    /// </summary>
    private string TruncateDescription(string description, int maxLength)
    {
        if (string.IsNullOrEmpty(description) || description.Length <= maxLength)
            return description;
        
        return description.Substring(0, maxLength) + "...";
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

    // DTOs for LLM relevance assessment
    private class RelevanceAssessment
    {
        public List<string> RelevantCommandNames { get; set; } = new();
        public string Reasoning { get; set; } = string.Empty;
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
