using System.ComponentModel;
using System.Text;
using System.Text.Json;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Serilog;

namespace WorkflowPlus.AIAgent.Tools;

/// <summary>
/// State-of-the-art query enhancement using HyDE and query expansion.
///
/// HyDE (Hypothetical Document Embeddings):
/// Instead of searching for the query directly, we generate a hypothetical answer
/// and search for documents similar to that answer. This significantly improves
/// retrieval quality for complex questions.
///
/// Query Expansion:
/// Generate multiple variations of the query to cover different phrasings.
///
/// Research: "2025's Ultimate Guide to RAG Retrieval" - improves recall by 20-30%
/// </summary>
public class QueryEnhancer
{
    private readonly IChatCompletionService _chatService;
    private readonly ILogger _logger;

    public QueryEnhancer(IChatCompletionService chatService)
    {
        _chatService = chatService;
        _logger = Log.ForContext<QueryEnhancer>();
    }

    /// <summary>
    /// Generate a hypothetical documentation snippet that would answer the query.
    /// This is the HyDE (Hypothetical Document Embeddings) technique.
    /// </summary>
    public async Task<string> GenerateHypotheticalDocumentAsync(string query)
    {
        _logger.Information("Generating hypothetical document for: {Query}", query);

        var prompt = $@"You are a technical documentation writer for a scripting language.

User's question: ""{query}""

Write a brief, technical documentation snippet (2-3 sentences) that would perfectly answer this question.
Focus on the technical details, syntax, and command names.
Do NOT include explanations or examples, just the core technical information.

Documentation snippet:";

        try
        {
            var response = await _chatService.GetChatMessageContentAsync(prompt);
            var hypotheticalDoc = response.Content?.Trim() ?? string.Empty;

            _logger.Debug("HyDE generated: {HypotheticalDoc}", hypotheticalDoc);
            return hypotheticalDoc;
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Failed to generate hypothetical document");
            return query; // Fallback to original query
        }
    }

    /// <summary>
    /// Expand the query into multiple variations to improve recall.
    /// This covers different ways users might phrase the same intent.
    /// </summary>
    public async Task<List<string>> ExpandQueryAsync(string query)
    {
        _logger.Information("Expanding query: {Query}", query);

        var prompt = $@"Generate 3 different variations of this search query, each using different technical terminology.

Original query: ""{query}""

Return ONLY a JSON array of strings, e.g., [""variation 1"", ""variation 2"", ""variation 3""]

Variations should:
- Use synonyms and alternative phrasings
- Cover both English and German technical terms if applicable
- Be concise (3-5 words each)

JSON array:";

        try
        {
            var response = await _chatService.GetChatMessageContentAsync(prompt);
            var content = response.Content?.Trim() ?? "[]";

            // Parse JSON array
            var variations = JsonSerializer.Deserialize<List<string>>(content);
            if (variations == null || variations.Count == 0)
            {
                _logger.Warning("Query expansion returned no variations");
                return new List<string> { query }; // Fallback
            }

            _logger.Debug("Expanded to {Count} variations", variations.Count);
            return variations;
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Failed to expand query");
            return new List<string> { query }; // Fallback to original
        }
    }

    /// <summary>
    /// Clean and optimize a query for search (remove redundant terms).
    /// </summary>
    public string CleanQuery(string query)
    {
        // Remove common redundant phrases
        var cleanedQuery = query
            .Replace("in Workflow+", "", StringComparison.OrdinalIgnoreCase)
            .Replace("in workflow", "", StringComparison.OrdinalIgnoreCase)
            .Replace("Workflow+", "", StringComparison.OrdinalIgnoreCase)
            .Replace("how do I", "", StringComparison.OrdinalIgnoreCase)
            .Replace("how to", "", StringComparison.OrdinalIgnoreCase)
            .Replace("can I", "", StringComparison.OrdinalIgnoreCase)
            .Trim();

        return cleanedQuery;
    }
}
