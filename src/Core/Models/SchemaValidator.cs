using System.Reflection;
using System.Text.Json;

namespace WorkflowPlus.AIAgent.Core.Models;

/// <summary>
/// Validates that the JSON schema matches the C# CodeReview model.
/// This ensures schema and model stay in sync during development.
/// </summary>
public static class SchemaValidator
{
    /// <summary>
    /// Validate that the JSON schema matches the CodeReview C# class.
    /// Throws InvalidOperationException if there's a mismatch.
    /// </summary>
    public static void ValidateSchema()
    {
        var schema = CodeReviewSchema.GetSchema();
        var schemaJson = JsonSerializer.Deserialize<JsonElement>(schema);

        if (!schemaJson.TryGetProperty("properties", out var properties))
        {
            throw new InvalidOperationException("Schema does not have 'properties' field");
        }

        // Validate that all required properties exist in CodeReview class
        var codeReviewType = typeof(CodeReview);

        foreach (var property in properties.EnumerateObject())
        {
            var propertyName = property.Name;
            var csharpPropertyName = ConvertToPascalCase(propertyName);

            var csharpProperty = codeReviewType.GetProperty(csharpPropertyName);
            if (csharpProperty == null)
            {
                throw new InvalidOperationException(
                    $"Schema property '{propertyName}' not found in CodeReview class as '{csharpPropertyName}'");
            }
        }

        // Validate CodeIssue schema
        ValidateCodeIssueSchema();

        Console.WriteLine("✅ Schema validation passed - CodeReview model matches JSON schema");
    }

    private static void ValidateCodeIssueSchema()
    {
        // Get the schema for code issues from the main schema
        var schema = CodeReviewSchema.GetSchema();
        var schemaJson = JsonSerializer.Deserialize<JsonElement>(schema);

        if (!schemaJson.TryGetProperty("properties", out var properties))
        {
            return;
        }

        // Check syntax_errors array items schema
        if (properties.TryGetProperty("syntax_errors", out var syntaxErrors) &&
            syntaxErrors.TryGetProperty("items", out var itemsSchema) &&
            itemsSchema.TryGetProperty("properties", out var issueProperties))
        {
            var codeIssueType = typeof(CodeIssue);

            foreach (var property in issueProperties.EnumerateObject())
            {
                var propertyName = property.Name;
                var csharpPropertyName = ConvertToPascalCase(propertyName);

                var csharpProperty = codeIssueType.GetProperty(csharpPropertyName);
                if (csharpProperty == null)
                {
                    throw new InvalidOperationException(
                        $"Schema property '{propertyName}' not found in CodeIssue class as '{csharpPropertyName}'");
                }
            }
        }
    }

    /// <summary>
    /// Convert snake_case to PascalCase.
    /// </summary>
    private static string ConvertToPascalCase(string snakeCase)
    {
        return string.Join("", snakeCase.Split('_')
            .Select(word => char.ToUpper(word[0]) + word.Substring(1)));
    }
}
