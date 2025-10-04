# Structured Outputs for Code Review

## Overview

The AI Agent now uses OpenAI's **Structured Outputs** feature for code review, replacing fragile JSON parsing with guaranteed valid JSON responses that match a predefined schema.

## What Changed

### Before (Fragile)
```csharp
// Old approach: String manipulation
var jsonStart = response.IndexOf('{');
var jsonEnd = response.LastIndexOf('}');
var json = response.Substring(jsonStart, jsonEnd - jsonStart + 1);
var review = JsonSerializer.Deserialize<CodeReview>(json); // Could fail!
```

### After (Reliable)
```csharp
// New approach: Structured Outputs with JSON Schema
var options = new ChatCompletionsOptions
{
    ResponseFormat = ChatCompletionsResponseFormat.CreateJsonSchemaFormat(
        jsonSchemaFormatName: "code_review",
        jsonSchema: CodeReviewSchema.GetSchema(),
        jsonSchemaIsStrict: true
    )
};

var response = await client.GetChatCompletionsAsync(options);
var review = JsonSerializer.Deserialize<CodeReview>(response.Content); // Always valid!
```

## Benefits

1. **Guaranteed Valid JSON**: OpenAI ensures the response matches the schema
2. **No String Manipulation**: Direct deserialization without parsing
3. **Type Safety**: Schema validation at runtime
4. **Better Reliability**: Eliminates parsing errors

## Configuration

Add to `agent_config.yml`:

```yaml
StructuredOutputs:
  Enabled: true                    # Enable/disable structured outputs
  StrictSchemaValidation: true     # Fail on schema mismatch
  FallbackOnError: true            # Fall back to legacy parsing on error
  MaxRetries: 3                    # Max retries for API calls
```

## Supported Models

Structured Outputs work with:
- `gpt-4o-mini` (all versions)
- `gpt-4o` (2024-08-06 and later)
- `gpt-4o-2024-08-06`

Older models (GPT-4, GPT-3.5) will automatically fall back to legacy parsing.

## Schema Definition

The JSON schema is defined in `CodeReviewSchema.cs`:

```csharp
{
  "is_approved": boolean,
  "confidence_score": integer (0-100),
  "syntax_errors": array of CodeIssue,
  "logic_issues": array of CodeIssue,
  "best_practice_violations": array of CodeIssue,
  "security_concerns": array of CodeIssue,
  "suggested_improvements": array of string,
  "overall_quality_score": integer (0-100)
}
```

Each `CodeIssue` has:
```csharp
{
  "line": integer (optional),
  "severity": "error" | "warning" | "info",
  "description": string,
  "suggested_fix": string (optional)
}
```

## Schema Validation

In DEBUG mode, the schema is validated on startup to ensure it matches the C# model:

```csharp
#if DEBUG
SchemaValidator.ValidateSchema();
#endif
```

This catches mismatches between the JSON schema and C# classes during development.

## Fallback Behavior

If structured outputs fail:
1. Log the error with full details
2. Fall back to legacy string parsing (if `FallbackOnError=true`)
3. Return a fallback review with error message (if all else fails)

## Error Handling

The implementation handles:
- **Schema validation errors** (400): Invalid schema sent to OpenAI
- **Rate limits** (429): OpenAI rate limit exceeded
- **Deserialization errors**: Should never happen with structured outputs
- **Model compatibility**: Automatic fallback for unsupported models

## Testing

Run the agent in DEBUG mode to validate the schema:

```bash
cd AiAgent/src/Agent
dotnet run
```

You should see:
```
✅ Schema validation passed - CodeReview model matches JSON schema
```

## Migration

The legacy parsing method is kept as a fallback and will be removed after 30 days of stable operation in production.

## References

- [OpenAI Structured Outputs Documentation](https://platform.openai.com/docs/guides/structured-outputs)
- [JSON Schema Specification](https://json-schema.org/)
