using System;
using System.Text.Json;

namespace WorkflowPlus.AIAgent.Core.Models;

/// <summary>
/// JSON Schema definition for structured code review output.
/// This schema is used with OpenAI's Structured Outputs feature to guarantee
/// valid JSON responses that match the CodeReview model.
/// 
/// This is the C# equivalent of Pydantic's model_json_schema() in Python.
/// </summary>
public static class CodeReviewSchema
{
    /// <summary>
    /// Get the JSON Schema for CodeReview as BinaryData for OpenAI API.
    /// </summary>
    public static BinaryData GetSchema()
    {
        var schema = new
        {
            type = "object",
            properties = new
            {
                is_approved = new
                {
                    type = "boolean",
                    description = "Whether the code is approved for use without modifications"
                },
                confidence_score = new
                {
                    type = "integer",
                    minimum = 0,
                    maximum = 100,
                    description = "Confidence in this review assessment (0-100)"
                },
                syntax_errors = new
                {
                    type = "array",
                    description = "List of syntax errors found in the code",
                    items = GetCodeIssueSchema()
                },
                logic_issues = new
                {
                    type = "array",
                    description = "List of logical problems or bugs in the code",
                    items = GetCodeIssueSchema()
                },
                best_practice_violations = new
                {
                    type = "array",
                    description = "List of code style or best practice violations",
                    items = GetCodeIssueSchema()
                },
                security_concerns = new
                {
                    type = "array",
                    description = "List of potential security vulnerabilities",
                    items = GetCodeIssueSchema()
                },
                suggested_improvements = new
                {
                    type = "array",
                    description = "General suggestions for improving the code",
                    items = new { type = "string" }
                },
                overall_quality_score = new
                {
                    type = "integer",
                    minimum = 0,
                    maximum = 100,
                    description = "Overall code quality score (0-100)"
                }
            },
            required = new[] { "is_approved", "confidence_score", "overall_quality_score" },
            additionalProperties = false
        };

        return BinaryData.FromObjectAsJson(schema);
    }

    /// <summary>
    /// Get the JSON Schema as a string (for OpenAI SDK v2.0).
    /// </summary>
    public static string GetSchemaAsString()
    {
        var schema = new
        {
            type = "object",
            properties = new
            {
                is_approved = new
                {
                    type = "boolean",
                    description = "Whether the code is approved for use without modifications"
                },
                confidence_score = new
                {
                    type = "integer",
                    minimum = 0,
                    maximum = 100,
                    description = "Confidence in this review assessment (0-100)"
                },
                syntax_errors = new
                {
                    type = "array",
                    description = "List of syntax errors found in the code",
                    items = GetCodeIssueSchema()
                },
                logic_issues = new
                {
                    type = "array",
                    description = "List of logical problems or bugs in the code",
                    items = GetCodeIssueSchema()
                },
                best_practice_violations = new
                {
                    type = "array",
                    description = "List of code style or best practice violations",
                    items = GetCodeIssueSchema()
                },
                security_concerns = new
                {
                    type = "array",
                    description = "List of potential security vulnerabilities",
                    items = GetCodeIssueSchema()
                },
                suggested_improvements = new
                {
                    type = "array",
                    description = "General suggestions for improving the code",
                    items = new { type = "string" }
                },
                overall_quality_score = new
                {
                    type = "integer",
                    minimum = 0,
                    maximum = 100,
                    description = "Overall code quality score (0-100)"
                }
            },
            required = new[] { "is_approved", "confidence_score", "overall_quality_score" },
            additionalProperties = false
        };

        return JsonSerializer.Serialize(schema);
    }

    /// <summary>
    /// Get the JSON Schema for a single code issue.
    /// </summary>
    private static object GetCodeIssueSchema()
    {
        return new
        {
            type = "object",
            properties = new
            {
                line = new
                {
                    type = "integer",
                    description = "Line number where the issue occurs (optional)"
                },
                severity = new
                {
                    type = "string",
                    @enum = new[] { "error", "warning", "info" },
                    description = "Severity level of the issue"
                },
                description = new
                {
                    type = "string",
                    description = "Human-readable description of the issue"
                },
                suggested_fix = new
                {
                    type = "string",
                    description = "Suggested fix for the issue (optional)"
                }
            },
            required = new[] { "severity", "description" },
            additionalProperties = false
        };
    }
}
