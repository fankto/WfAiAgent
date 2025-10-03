using System.ComponentModel;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.SemanticKernel;
using Serilog;

namespace WorkflowPlus.AIAgent.Tools;

/// <summary>
/// Tool for finding and matching example scripts from documentation
/// </summary>
public class ExampleScriptMatcher
{
    private readonly HttpClient _httpClient;
    private readonly ILogger _logger;
    private const string AiSearchBaseUrl = "http://localhost:54321";

    public ExampleScriptMatcher()
    {
        _httpClient = new HttpClient
        {
            BaseAddress = new Uri(AiSearchBaseUrl),
            Timeout = TimeSpan.FromSeconds(10)
        };
        _logger = Log.ForContext<ExampleScriptMatcher>();
    }

    [KernelFunction("find_similar_examples")]
    [Description("Find example scripts similar to the user's request. Use this to get syntax patterns and real code examples.")]
    public async Task<string> FindSimilarExamplesAsync(
        [Description("User's natural language request describing what they want to do")] string query,
        [Description("Maximum examples to return (default 3)")] int maxResults = 3)
    {
        _logger.Information("Searching for similar examples: {Query}", query);

        try
        {
            // Check if example index is available
            var statusResponse = await _httpClient.GetAsync("/examples/status");
            if (statusResponse.IsSuccessStatusCode)
            {
                var statusJson = await statusResponse.Content.ReadAsStringAsync();
                var status = JsonSerializer.Deserialize<ExampleStatusResponse>(statusJson, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                if (status?.Available == false)
                {
                    _logger.Warning("Example index not available");
                    return "Example index is not yet built. Please build the example index first.";
                }
            }

            // Search for examples
            var url = $"/examples/search?query={Uri.EscapeDataString(query)}&maxResults={maxResults}&minScore=0.5";
            var response = await _httpClient.GetAsync(url);

            if (!response.IsSuccessStatusCode)
            {
                _logger.Warning("Example search returned status {Status}", response.StatusCode);
                return "Unable to search examples at this time.";
            }

            var jsonResponse = await response.Content.ReadAsStringAsync();
            var searchResults = JsonSerializer.Deserialize<ExampleSearchResponse>(jsonResponse, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            if (searchResults == null || searchResults.Results.Count == 0)
            {
                _logger.Information("No examples found for query: {Query}", query);
                return $"No similar examples found for: {query}";
            }

            // Format results for LLM consumption
            var sb = new StringBuilder();
            sb.AppendLine($"Found {searchResults.TotalResults} similar examples:\n");

            foreach (var result in searchResults.Results)
            {
                sb.AppendLine($"## Example from {result.SourceFile}");
                sb.AppendLine($"**Relevance Score:** {result.Score:F2}");
                
                if (!string.IsNullOrEmpty(result.Context))
                {
                    sb.AppendLine($"**Context:** {result.Context}");
                }
                
                if (result.CommandsUsed.Any())
                {
                    sb.AppendLine($"**Commands Used:** {string.Join(", ", result.CommandsUsed)}");
                }
                
                sb.AppendLine($"**Code:**");
                sb.AppendLine("```");
                sb.AppendLine(result.Code);
                sb.AppendLine("```");
                sb.AppendLine();
            }

            _logger.Information("Returning {Count} example matches", searchResults.Results.Count);
            return sb.ToString();
        }
        catch (HttpRequestException ex)
        {
            _logger.Error(ex, "HTTP error searching examples");
            return "The example search service is currently unavailable.";
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Error searching examples");
            return "An error occurred while searching examples.";
        }
    }

    /// <summary>
    /// Extract syntax patterns from examples
    /// </summary>
    public async Task<SyntaxPatterns> ExtractPatternsAsync(List<ExampleMatchDto> examples)
    {
        var patterns = new SyntaxPatterns();

        foreach (var example in examples)
        {
            // Extract variable declarations (Declare X as Type)
            var declareMatches = Regex.Matches(example.Code, @"Declare\s+(\w+)\s+as\s+(\w+)", RegexOptions.IgnoreCase);
            foreach (Match match in declareMatches)
            {
                patterns.VariableDeclarations.Add(match.Value);
            }

            // Extract function calls (FunctionName(...))
            var functionMatches = Regex.Matches(example.Code, @"(\w+)\s*\([^)]*\)");
            foreach (Match match in functionMatches)
            {
                patterns.FunctionCalls.Add(match.Value);
            }

            // Extract control flow patterns
            var controlFlowMatches = Regex.Matches(example.Code, 
                @"(If\s+.+?\s+Then|While\s+.+|For\s+.+?\s+To\s+.+|End\s+If|End\s+While|End\s+For)", 
                RegexOptions.IgnoreCase);
            foreach (Match match in controlFlowMatches)
            {
                patterns.ControlFlowPatterns.Add(match.Value);
            }

            // Extract error handling patterns
            var errorHandlingMatches = Regex.Matches(example.Code,
                @"(Try|Catch|Finally|End\s+Try|On\s+Error)",
                RegexOptions.IgnoreCase);
            foreach (Match match in errorHandlingMatches)
            {
                patterns.ErrorHandlingPatterns.Add(match.Value);
            }
        }

        // Deduplicate patterns
        patterns.VariableDeclarations = patterns.VariableDeclarations.Distinct().ToList();
        patterns.FunctionCalls = patterns.FunctionCalls.Distinct().ToList();
        patterns.ControlFlowPatterns = patterns.ControlFlowPatterns.Distinct().ToList();
        patterns.ErrorHandlingPatterns = patterns.ErrorHandlingPatterns.Distinct().ToList();

        _logger.Information("Extracted patterns: {VarCount} variables, {FuncCount} functions, {ControlCount} control flow",
            patterns.VariableDeclarations.Count, patterns.FunctionCalls.Count, patterns.ControlFlowPatterns.Count);

        return patterns;
    }

    // DTOs matching AiSearch API response
    private class ExampleSearchResponse
    {
        public List<ExampleMatchDto> Results { get; set; } = new();
        public int TotalResults { get; set; }
    }

    private class ExampleStatusResponse
    {
        public bool Available { get; set; }
    }
}

/// <summary>
/// DTO for example match
/// </summary>
public class ExampleMatchDto
{
    public string Id { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public string SourceFile { get; set; } = string.Empty;
    public List<string> CommandsUsed { get; set; } = new();
    public string Context { get; set; } = string.Empty;
    public float Score { get; set; }
}

/// <summary>
/// Extracted syntax patterns from examples
/// </summary>
public class SyntaxPatterns
{
    public List<string> VariableDeclarations { get; set; } = new();
    public List<string> FunctionCalls { get; set; } = new();
    public List<string> ControlFlowPatterns { get; set; } = new();
    public List<string> ErrorHandlingPatterns { get; set; } = new();
}
