using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using WorkflowPlus.AIAgent.Core.Models;

namespace WorkflowPlus.AIAgent.Core.Validation;

/// <summary>
/// Validates that JSON Schema matches C# model (like Pydantic's schema validation).
/// </summary>
public static class SchemaValidator
{
    /// <summary>
    /// Validate that the JSON Schema matches the CodeReview C# model.
    /// This ensures schema and model stay in sync (like Pydantic does automatically).
    /// </summary>
    public static void ValidateSchema()
    {
        var schemaJson = CodeReviewSchema.GetSchemaAsString();
        var schema = JsonSerializer.Deserialize<JsonElement>(schemaJson);

        if (!schema.TryGetProperty("properties", out var properties))
        {
            throw new InvalidOperationException("Schema missing 'properties' field");
        }

        var codeReviewType = typeof(CodeReview);
        var csharpProperties = codeReviewType.GetProperties(BindingFlags.Public | BindingFlags.Instance);

        // Check that all schema properties exist in C# model
        foreach (var property in properties.EnumerateObject())
        {
            var schemaPropertyName = property.Name;
            
            // Find corresponding C# property by JsonPropertyName attribute
            var csharpProperty = csharpProperties.FirstOrDefault(p =>
            {
                var jsonAttr = p.GetCustomAttribute<JsonPropertyNameAttribute>();
                return jsonAttr?.Name == schemaPropertyName;
            });

            if (csharpProperty == null)
            {
                throw new InvalidOperationException(
                    $"Schema property '{schemaPropertyName}' not found in CodeReview class. " +
                    $"Add a property with [JsonPropertyName(\"{schemaPropertyName}\")]");
            }

            // Validate type compatibility
            ValidatePropertyType(property.Value, csharpProperty);
        }

        // Check that all required C# properties are in schema
        foreach (var csharpProperty in csharpProperties)
        {
            var jsonAttr = csharpProperty.GetCustomAttribute<JsonPropertyNameAttribute>();
            if (jsonAttr == null)
                continue;

            var schemaPropertyName = jsonAttr.Name;
            if (!properties.TryGetProperty(schemaPropertyName, out _))
            {
                throw new InvalidOperationException(
                    $"C# property '{csharpProperty.Name}' (JSON: '{schemaPropertyName}') " +
                    $"not found in schema");
            }
        }

        Console.WriteLine("✅ Schema validation passed - CodeReview model matches JSON schema");
    }

    private static void ValidatePropertyType(JsonElement schemaProperty, PropertyInfo csharpProperty)
    {
        if (!schemaProperty.TryGetProperty("type", out var typeElement))
            return;

        var schemaType = typeElement.GetString();
        var csharpType = csharpProperty.PropertyType;

        // Basic type checking
        var isValid = schemaType switch
        {
            "boolean" => csharpType == typeof(bool),
            "integer" => csharpType == typeof(int) || csharpType == typeof(int?),
            "string" => csharpType == typeof(string),
            "array" => csharpType.IsGenericType && 
                      csharpType.GetGenericTypeDefinition() == typeof(List<>),
            "object" => csharpType.IsClass,
            _ => true // Unknown type, skip validation
        };

        if (!isValid)
        {
            throw new InvalidOperationException(
                $"Type mismatch for property '{csharpProperty.Name}': " +
                $"Schema expects '{schemaType}' but C# has '{csharpType.Name}'");
        }
    }
}
