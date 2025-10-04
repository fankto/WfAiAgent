using System.Text.Json;
using WorkflowPlus.AIAgent.Core.Models;
using Xunit;

namespace WorkflowPlus.AIAgent.Tests.Unit;

/// <summary>
/// Unit tests for CodeReview schema validation and deserialization.
/// </summary>
public class CodeReviewSchemaTests
{
    [Fact]
    public void CodeReviewSchema_GetSchema_ReturnsValidBinaryData()
    {
        // Act
        var schema = CodeReviewSchema.GetSchema();

        // Assert
        Assert.NotNull(schema);
        Assert.True(schema.ToMemory().Length > 0);
    }

    [Fact]
    public void CodeReviewSchema_GetSchema_ContainsRequiredProperties()
    {
        // Act
        var schema = CodeReviewSchema.GetSchema();
        var schemaJson = JsonSerializer.Deserialize<JsonElement>(schema);

        // Assert
        Assert.True(schemaJson.TryGetProperty("properties", out var properties));
        Assert.True(properties.TryGetProperty("is_approved", out _));
        Assert.True(properties.TryGetProperty("confidence_score", out _));
        Assert.True(properties.TryGetProperty("syntax_errors", out _));
        Assert.True(properties.TryGetProperty("logic_issues", out _));
        Assert.True(properties.TryGetProperty("best_practice_violations", out _));
        Assert.True(properties.TryGetProperty("security_concerns", out _));
        Assert.True(properties.TryGetProperty("suggested_improvements", out _));
        Assert.True(properties.TryGetProperty("overall_quality_score", out _));
    }

    [Fact]
    public void CodeReviewSchema_GetSchema_HasCorrectRequiredFields()
    {
        // Act
        var schema = CodeReviewSchema.GetSchema();
        var schemaJson = JsonSerializer.Deserialize<JsonElement>(schema);

        // Assert
        Assert.True(schemaJson.TryGetProperty("required", out var required));
        var requiredArray = required.EnumerateArray().Select(e => e.GetString()).ToList();
        
        Assert.Contains("is_approved", requiredArray);
        Assert.Contains("confidence_score", requiredArray);
        Assert.Contains("overall_quality_score", requiredArray);
    }

    [Fact]
    public void CodeReview_Deserialization_WithValidJson_Succeeds()
    {
        // Arrange
        var json = @"{
            ""is_approved"": true,
            ""confidence_score"": 85,
            ""syntax_errors"": [],
            ""logic_issues"": [],
            ""best_practice_violations"": [],
            ""security_concerns"": [],
            ""suggested_improvements"": [""Add error handling""],
            ""overall_quality_score"": 80
        }";

        // Act
        var review = JsonSerializer.Deserialize<CodeReview>(json, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower
        });

        // Assert
        Assert.NotNull(review);
        Assert.True(review.IsApproved);
        Assert.Equal(85, review.ConfidenceScore);
        Assert.Equal(80, review.OverallQualityScore);
        Assert.Single(review.SuggestedImprovements);
        Assert.Equal("Add error handling", review.SuggestedImprovements[0]);
    }

    [Fact]
    public void CodeReview_Deserialization_WithIssues_Succeeds()
    {
        // Arrange
        var json = @"{
            ""is_approved"": false,
            ""confidence_score"": 90,
            ""syntax_errors"": [
                {
                    ""line"": 5,
                    ""severity"": ""error"",
                    ""description"": ""Missing semicolon"",
                    ""suggested_fix"": ""Add semicolon at end of line""
                }
            ],
            ""logic_issues"": [
                {
                    ""severity"": ""warning"",
                    ""description"": ""Potential null reference""
                }
            ],
            ""best_practice_violations"": [],
            ""security_concerns"": [],
            ""suggested_improvements"": [],
            ""overall_quality_score"": 60
        }";

        // Act
        var review = JsonSerializer.Deserialize<CodeReview>(json, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower
        });

        // Assert
        Assert.NotNull(review);
        Assert.False(review.IsApproved);
        Assert.Single(review.SyntaxErrors);
        Assert.Equal(5, review.SyntaxErrors[0].Line);
        Assert.Equal("error", review.SyntaxErrors[0].Severity);
        Assert.Equal("Missing semicolon", review.SyntaxErrors[0].Description);
        Assert.Equal("Add semicolon at end of line", review.SyntaxErrors[0].SuggestedFix);
        
        Assert.Single(review.LogicIssues);
        Assert.Null(review.LogicIssues[0].Line);
        Assert.Equal("warning", review.LogicIssues[0].Severity);
    }

    [Fact]
    public void CodeReview_HasCriticalIssues_WithErrorSeverity_ReturnsTrue()
    {
        // Arrange
        var review = new CodeReview
        {
            SyntaxErrors = new List<CodeIssue>
            {
                new CodeIssue { Severity = "error", Description = "Syntax error" }
            }
        };

        // Act
        var hasCritical = review.HasCriticalIssues();

        // Assert
        Assert.True(hasCritical);
    }

    [Fact]
    public void CodeReview_HasCriticalIssues_WithWarningOnly_ReturnsFalse()
    {
        // Arrange
        var review = new CodeReview
        {
            SyntaxErrors = new List<CodeIssue>
            {
                new CodeIssue { Severity = "warning", Description = "Minor issue" }
            }
        };

        // Act
        var hasCritical = review.HasCriticalIssues();

        // Assert
        Assert.False(hasCritical);
    }

    [Fact]
    public void CodeReview_TotalIssueCount_ReturnsCorrectSum()
    {
        // Arrange
        var review = new CodeReview
        {
            SyntaxErrors = new List<CodeIssue> { new(), new() },
            LogicIssues = new List<CodeIssue> { new() },
            BestPracticeViolations = new List<CodeIssue> { new(), new(), new() },
            SecurityConcerns = new List<CodeIssue> { new() }
        };

        // Act
        var total = review.TotalIssueCount();

        // Assert
        Assert.Equal(7, total);
    }

    [Fact]
    public void SchemaValidator_ValidateSchema_DoesNotThrow()
    {
        // Act & Assert
        var exception = Record.Exception(() => SchemaValidator.ValidateSchema());
        Assert.Null(exception);
    }

    [Fact]
    public void CodeIssue_OptionalFields_CanBeNull()
    {
        // Arrange
        var json = @"{
            ""severity"": ""info"",
            ""description"": ""Test issue""
        }";

        // Act
        var issue = JsonSerializer.Deserialize<CodeIssue>(json, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower
        });

        // Assert
        Assert.NotNull(issue);
        Assert.Null(issue.Line);
        Assert.Null(issue.SuggestedFix);
        Assert.Equal("info", issue.Severity);
        Assert.Equal("Test issue", issue.Description);
    }

    [Theory]
    [InlineData("error")]
    [InlineData("warning")]
    [InlineData("info")]
    public void CodeIssue_Severity_AcceptsValidValues(string severity)
    {
        // Arrange
        var json = $@"{{
            ""severity"": ""{severity}"",
            ""description"": ""Test""
        }}";

        // Act
        var issue = JsonSerializer.Deserialize<CodeIssue>(json, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower
        });

        // Assert
        Assert.NotNull(issue);
        Assert.Equal(severity, issue.Severity);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(50)]
    [InlineData(100)]
    public void CodeReview_Scores_AcceptsValidRange(int score)
    {
        // Arrange
        var json = $@"{{
            ""is_approved"": true,
            ""confidence_score"": {score},
            ""syntax_errors"": [],
            ""logic_issues"": [],
            ""best_practice_violations"": [],
            ""security_concerns"": [],
            ""suggested_improvements"": [],
            ""overall_quality_score"": {score}
        }}";

        // Act
        var review = JsonSerializer.Deserialize<CodeReview>(json, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower
        });

        // Assert
        Assert.NotNull(review);
        Assert.Equal(score, review.ConfidenceScore);
        Assert.Equal(score, review.OverallQualityScore);
    }
}
