using System.Text.Json.Serialization;

namespace WorkflowPlus.AIAgent.Core.Models;

/// <summary>
/// Structured code review result using JSON schema for reliability.
/// This is a state-of-the-art pattern for LLM-based code validation.
/// </summary>
public class CodeReview
{
    [JsonPropertyName("is_approved")]
    public bool IsApproved { get; set; }

    [JsonPropertyName("confidence_score")]
    public int ConfidenceScore { get; set; } // 0-100

    [JsonPropertyName("syntax_errors")]
    public List<CodeIssue> SyntaxErrors { get; set; } = new();

    [JsonPropertyName("logic_issues")]
    public List<CodeIssue> LogicIssues { get; set; } = new();

    [JsonPropertyName("best_practice_violations")]
    public List<CodeIssue> BestPracticeViolations { get; set; } = new();

    [JsonPropertyName("security_concerns")]
    public List<CodeIssue> SecurityConcerns { get; set; } = new();

    [JsonPropertyName("suggested_improvements")]
    public List<string> SuggestedImprovements { get; set; } = new();

    [JsonPropertyName("overall_quality_score")]
    public int OverallQualityScore { get; set; } // 0-100

    public bool HasCriticalIssues()
    {
        return SyntaxErrors.Any(e => e.Severity == "critical") ||
               LogicIssues.Any(e => e.Severity == "critical") ||
               SecurityConcerns.Any();
    }

    public int TotalIssueCount()
    {
        return SyntaxErrors.Count + LogicIssues.Count +
               BestPracticeViolations.Count + SecurityConcerns.Count;
    }
}

public class CodeIssue
{
    [JsonPropertyName("line")]
    public int? Line { get; set; }

    [JsonPropertyName("severity")]
    public string Severity { get; set; } = "info"; // info, warning, error, critical

    [JsonPropertyName("description")]
    public string Description { get; set; } = string.Empty;

    [JsonPropertyName("suggested_fix")]
    public string? SuggestedFix { get; set; }
}
