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
    
    // Terms that should be removed from search queries (noise words)
    private static readonly string[] NoiseTerms = new[]
    {
        // Product names
        "workflow+", "workflowplus", "workflow plus",
        
        // Generic programming terms
        "script", "code", "function", "command", "program",
        
        // Prepositions and articles that add no semantic value
        "in", "using", "with", "for", "the", "a", "an",
        
        // Phrases that add context but hurt search
        "how to", "how do i", "i want to", "i need to", "can you",
        "help me", "please", "erstell mir", "wie kann ich"
    };

    // Core action verbs that should be preserved in queries
    private static readonly string[] CoreActionVerbs = new[]
    {
        "create", "new", "make", "initialize",
        "add", "insert", "append", "push",
        "delete", "remove", "clear", "drop",
        "update", "modify", "change", "set",
        "get", "fetch", "retrieve", "read",
        "sort", "order", "arrange",
        "search", "find", "query", "filter",
        "send", "mail", "email", "notify",
        "open", "close", "connect", "disconnect",
        "execute", "run", "call", "invoke"
    };
    
    // Core nouns that represent data structures or entities
    private static readonly string[] CoreNouns = new[]
    {
        "array", "list", "collection",
        "database", "table", "record", "row",
        "file", "document", "folder", "directory",
        "string", "text", "number", "date",
        "email", "mail", "message",
        "user", "customer", "person",
        "connection", "session"
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
    [Description("Search documentation for commands and functions. Extract ONLY the core action and target from the user's request (e.g., 'create array', 'sort list', 'insert database'). Do NOT add product names or generic terms like 'script' or 'code'.")]
    public async Task<string> SearchCommandsAsync(
        [Description("Minimal technical query with only action verb and target noun (e.g., 'create array', 'send email', 'sort list')")] string query,
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
    /// Formulate a clean search query by extracting only core technical concepts.
    /// This is the critical method that determines search quality.
    /// 
    /// Strategy:
    /// 1. Extract action verbs (create, sort, add, etc.)
    /// 2. Extract target nouns (array, database, file, etc.)
    /// 3. Remove all noise words (product names, generic terms, filler words)
    /// 4. Keep it minimal - typically 2-3 words maximum
    /// </summary>
    public string FormulateQuery(string userInput)
    {
        var normalized = userInput.ToLowerInvariant();
        
        // Step 1: Remove noise terms first (most aggressive filtering)
        foreach (var noiseTerm in NoiseTerms)
        {
            // Use word boundaries to avoid removing parts of words
            var pattern = $@"\b{Regex.Escape(noiseTerm)}\b";
            normalized = Regex.Replace(normalized, pattern, " ", RegexOptions.IgnoreCase);
        }
        
        // Step 2: Tokenize into words
        var words = Regex.Split(normalized, @"\s+")
            .Where(w => !string.IsNullOrWhiteSpace(w) && w.Length > 1)
            .ToList();
        
        // Step 3: Extract core concepts (verbs + nouns)
        var extractedVerbs = new List<string>();
        var extractedNouns = new List<string>();
        
        foreach (var word in words)
        {
            // Check if it's a core action verb
            if (CoreActionVerbs.Any(v => word.Contains(v) || v.Contains(word)))
            {
                var matchedVerb = CoreActionVerbs.FirstOrDefault(v => 
                    word.Contains(v) || v.Contains(word) || LevenshteinDistance(word, v) <= 2);
                if (matchedVerb != null && !extractedVerbs.Contains(matchedVerb))
                {
                    extractedVerbs.Add(matchedVerb);
                }
            }
            
            // Check if it's a core noun
            if (CoreNouns.Any(n => word.Contains(n) || n.Contains(word)))
            {
                var matchedNoun = CoreNouns.FirstOrDefault(n => 
                    word.Contains(n) || n.Contains(word) || LevenshteinDistance(word, n) <= 2);
                if (matchedNoun != null && !extractedNouns.Contains(matchedNoun))
                {
                    extractedNouns.Add(matchedNoun);
                }
            }
        }
        
        // Step 4: Build minimal query (verb + noun pattern)
        var queryParts = new List<string>();
        
        // Prefer the first verb (usually the main action)
        if (extractedVerbs.Any())
        {
            queryParts.Add(extractedVerbs.First());
        }
        
        // Add all relevant nouns (but limit to 2 for precision)
        queryParts.AddRange(extractedNouns.Take(2));
        
        // Step 5: Fallback - if we extracted nothing, keep the most meaningful words
        if (!queryParts.Any())
        {
            // Keep words that are longer than 3 characters and not common stop words
            var stopWords = new[] { "that", "this", "what", "when", "where", "which", "who", "why", "how" };
            queryParts = words
                .Where(w => w.Length > 3 && !stopWords.Contains(w))
                .Take(3)
                .ToList();
        }
        
        var formulatedQuery = string.Join(" ", queryParts).Trim();
        
        // Ensure we have something to search for
        if (string.IsNullOrWhiteSpace(formulatedQuery))
        {
            formulatedQuery = string.Join(" ", words.Take(3));
        }
        
        _logger.Debug("Formulated query: '{Original}' -> '{Formulated}'", userInput, formulatedQuery);
        
        return formulatedQuery;
    }
    
    /// <summary>
    /// Calculate Levenshtein distance for fuzzy matching
    /// </summary>
    private int LevenshteinDistance(string s1, string s2)
    {
        if (string.IsNullOrEmpty(s1)) return s2?.Length ?? 0;
        if (string.IsNullOrEmpty(s2)) return s1.Length;
        
        var d = new int[s1.Length + 1, s2.Length + 1];
        
        for (int i = 0; i <= s1.Length; i++) d[i, 0] = i;
        for (int j = 0; j <= s2.Length; j++) d[0, j] = j;
        
        for (int i = 1; i <= s1.Length; i++)
        {
            for (int j = 1; j <= s2.Length; j++)
            {
                int cost = (s2[j - 1] == s1[i - 1]) ? 0 : 1;
                d[i, j] = Math.Min(Math.Min(d[i - 1, j] + 1, d[i, j - 1] + 1), d[i - 1, j - 1] + cost);
            }
        }
        
        return d[s1.Length, s2.Length];
    }

    /// <summary>
    /// Break down complex queries into simpler sub-queries for refinement.
    /// Strategy: Try progressively simpler variations.
    /// </summary>
    private List<string> BreakDownQuery(string query)
    {
        var subQueries = new List<string>();
        var words = query.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        
        // Strategy 1: If query has multiple words, try each word individually
        if (words.Length > 1)
        {
            // Try the first word (usually the verb/action)
            subQueries.Add(words[0]);
            
            // Try the last word (usually the noun/target)
            if (words.Length > 1)
            {
                subQueries.Add(words[^1]);
            }
            
            // Try first two words
            if (words.Length >= 2)
            {
                subQueries.Add($"{words[0]} {words[1]}");
            }
        }
        
        // Strategy 2: Try synonyms/variations for common terms
        var synonymMap = new Dictionary<string, string[]>
        {
            { "create", new[] { "new", "make", "initialize" } },
            { "add", new[] { "insert", "append", "push" } },
            { "sort", new[] { "order", "arrange" } },
            { "array", new[] { "list", "collection" } },
            { "database", new[] { "table", "db" } },
            { "delete", new[] { "remove", "clear" } }
        };
        
        foreach (var word in words)
        {
            if (synonymMap.TryGetValue(word.ToLower(), out var synonyms))
            {
                foreach (var synonym in synonyms)
                {
                    // Replace the word with its synonym
                    var synonymQuery = query.Replace(word, synonym, StringComparison.OrdinalIgnoreCase);
                    if (synonymQuery != query)
                    {
                        subQueries.Add(synonymQuery);
                    }
                }
            }
        }
        
        // Strategy 3: Try just the core nouns if we have them
        var foundNouns = words.Where(w => CoreNouns.Contains(w.ToLower())).ToList();
        if (foundNouns.Any())
        {
            subQueries.Add(string.Join(" ", foundNouns));
        }
        
        // Deduplicate and return
        return subQueries
            .Where(q => !string.IsNullOrWhiteSpace(q))
            .Distinct()
            .Take(5) // Limit to 5 refinement attempts
            .ToList();
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
