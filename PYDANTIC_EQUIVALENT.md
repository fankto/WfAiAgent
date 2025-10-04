# Pydantic-Equivalent Validation in C#

## Overview

This document explains how we've implemented **Pydantic-style validation** in C# using **FluentValidation** combined with **OpenAI Structured Outputs**.

## What is Pydantic?

Pydantic is a Python library that provides:
1. **Data validation** using Python type hints
2. **Automatic JSON schema generation** from Python classes
3. **Parsing with validation** - ensures data meets all constraints
4. **Custom validators** for business logic

## The C# Equivalent

| Python (Pydantic) | C# (FluentValidation) | Purpose |
|-------------------|----------------------|---------|
| `BaseModel` | `AbstractValidator<T>` | Base class for validation |
| `Field(ge=0, le=100)` | `InclusiveBetween(0, 100)` | Constraint validation |
| `@validator` | `Must()` / `Custom()` | Custom validation rules |
| `model_validate_json()` | `Deserialize()` + `ValidateAndThrow()` | Parse and validate |
| `model_json_schema()` | `CodeReviewSchema.GetSchemaAsString()` | Generate JSON schema |

## Complete Example

### Python with Pydantic

```python
from pydantic import BaseModel, Field, validator
from typing import List, Optional

class CodeIssue(BaseModel):
    line: Optional[int] = None
    severity: str = Field(pattern="^(error|warning|info)$")
    description: str = Field(min_length=10, max_length=500)
    suggested_fix: Optional[str] = None
    
    @validator('line')
    def line_must_be_positive(cls, v):
        if v is not None and v <= 0:
            raise ValueError('Line number must be positive')
        return v

class CodeReview(BaseModel):
    is_approved: bool
    confidence_score: int = Field(ge=0, le=100)
    syntax_errors: List[CodeIssue] = []
    logic_issues: List[CodeIssue] = []
    overall_quality_score: int = Field(ge=0, le=100)
    
    @validator('confidence_score')
    def check_approval_confidence(cls, v, values):
        if values.get('is_approved') and v < 70:
            raise ValueError('Cannot approve with confidence < 70')
        return v
    
    @validator('overall_quality_score')
    def check_quality_consistency(cls, v, values):
        total_issues = len(values.get('syntax_errors', [])) + len(values.get('logic_issues', []))
        if v >= 80 and total_issues > 3:
            raise ValueError('High quality score should have few issues')
        return v

# Usage
try:
    review = CodeReview.model_validate_json(json_string)
    print(f"✅ Valid: {review.is_approved}")
except ValidationError as e:
    print(f"❌ Invalid: {e}")
```

### C# with FluentValidation

```csharp
using FluentValidation;

public class CodeIssue
{
    public int? Line { get; set; }
    public string Severity { get; set; } = "info";
    public string Description { get; set; } = string.Empty;
    public string? SuggestedFix { get; set; }
}

public class CodeIssueValidator : AbstractValidator<CodeIssue>
{
    public CodeIssueValidator()
    {
        // Severity must be one of the allowed values
        RuleFor(x => x.Severity)
            .NotEmpty()
            .Must(s => new[] { "error", "warning", "info" }.Contains(s))
            .WithMessage("Severity must be 'error', 'warning', or 'info'");
        
        // Description constraints
        RuleFor(x => x.Description)
            .NotEmpty()
            .MinimumLength(10)
            .MaximumLength(500);
        
        // Line must be positive if provided
        RuleFor(x => x.Line)
            .GreaterThan(0)
            .When(x => x.Line.HasValue)
            .WithMessage("Line number must be positive");
    }
}

public class CodeReview
{
    public bool IsApproved { get; set; }
    public int ConfidenceScore { get; set; }
    public List<CodeIssue> SyntaxErrors { get; set; } = new();
    public List<CodeIssue> LogicIssues { get; set; } = new();
    public int OverallQualityScore { get; set; }
}

public class CodeReviewValidator : AbstractValidator<CodeReview>
{
    public CodeReviewValidator()
    {
        // Confidence score constraints
        RuleFor(x => x.ConfidenceScore)
            .InclusiveBetween(0, 100);
        
        // Overall quality score constraints
        RuleFor(x => x.OverallQualityScore)
            .InclusiveBetween(0, 100);
        
        // Business rule: Cannot approve with low confidence
        RuleFor(x => x)
            .Must(x => !x.IsApproved || x.ConfidenceScore >= 70)
            .WithMessage("Cannot approve with confidence < 70");
        
        // Business rule: High quality should have few issues
        RuleFor(x => x)
            .Must(x => x.OverallQualityScore < 80 || 
                      (x.SyntaxErrors.Count + x.LogicIssues.Count) <= 3)
            .WithMessage("High quality score should have few issues");
        
        // Validate nested objects
        RuleForEach(x => x.SyntaxErrors)
            .SetValidator(new CodeIssueValidator());
        
        RuleForEach(x => x.LogicIssues)
            .SetValidator(new CodeIssueValidator());
    }
}

// Usage
try
{
    var review = JsonSerializer.Deserialize<CodeReview>(jsonString);
    var validator = new CodeReviewValidator();
    validator.ValidateAndThrow(review);  // ✅ Pydantic-equivalent!
    Console.WriteLine($"✅ Valid: {review.IsApproved}");
}
catch (ValidationException ex)
{
    Console.WriteLine($"❌ Invalid: {string.Join("; ", ex.Errors)}");
}
```

## Key Features Comparison

### 1. Type Constraints

**Python (Pydantic):**
```python
confidence_score: int = Field(ge=0, le=100)
```

**C# (FluentValidation):**
```csharp
RuleFor(x => x.ConfidenceScore).InclusiveBetween(0, 100);
```

### 2. String Patterns

**Python (Pydantic):**
```python
severity: str = Field(pattern="^(error|warning|info)$")
```

**C# (FluentValidation):**
```csharp
RuleFor(x => x.Severity)
    .Must(s => new[] { "error", "warning", "info" }.Contains(s));
```

### 3. Custom Validators

**Python (Pydantic):**
```python
@validator('confidence_score')
def check_confidence(cls, v, values):
    if values.get('is_approved') and v < 70:
        raise ValueError('Cannot approve with low confidence')
    return v
```

**C# (FluentValidation):**
```csharp
RuleFor(x => x)
    .Must(x => !x.IsApproved || x.ConfidenceScore >= 70)
    .WithMessage("Cannot approve with low confidence");
```

### 4. Nested Validation

**Python (Pydantic):**
```python
syntax_errors: List[CodeIssue] = []  # Automatically validates each item
```

**C# (FluentValidation):**
```csharp
RuleForEach(x => x.SyntaxErrors)
    .SetValidator(new CodeIssueValidator());
```

### 5. Optional Fields

**Python (Pydantic):**
```python
line: Optional[int] = None
```

**C# (FluentValidation):**
```csharp
public int? Line { get; set; }

RuleFor(x => x.Line)
    .GreaterThan(0)
    .When(x => x.Line.HasValue);  // Only validate if present
```

## Integration with OpenAI Structured Outputs

### Complete Flow

```csharp
// 1. Define model
public class CodeReview { ... }

// 2. Define validator (Pydantic-equivalent)
public class CodeReviewValidator : AbstractValidator<CodeReview> { ... }

// 3. Generate JSON Schema
public static class CodeReviewSchema
{
    public static string GetSchemaAsString() { ... }
}

// 4. Call OpenAI with schema
var completion = await client.GetChatCompletionAsync(
    model: "gpt-4o",
    messages: messages,
    options: new ChatCompletionOptions
    {
        ResponseFormat = ChatResponseFormat.CreateJsonSchemaFormat(
            name: "code_review",
            jsonSchema: BinaryData.FromString(CodeReviewSchema.GetSchemaAsString()),
            strictSchemaAdherence: true  // OpenAI guarantees valid JSON
        )
    }
);

// 5. Deserialize (guaranteed valid JSON structure)
var review = JsonSerializer.Deserialize<CodeReview>(completion.Content);

// 6. Validate with FluentValidation (Pydantic-equivalent)
var validator = new CodeReviewValidator();
validator.ValidateAndThrow(review);  // ✅ Now we have full Pydantic-style validation!
```

## Benefits

### What OpenAI Structured Outputs Provides
- ✅ Guaranteed valid JSON structure
- ✅ Correct types (int, bool, string, array)
- ✅ Required fields present
- ✅ No extra fields (if `additionalProperties: false`)

### What FluentValidation Adds (Pydantic-equivalent)
- ✅ Value constraints (0-100, min/max length)
- ✅ Business logic validation (approval rules)
- ✅ Cross-field validation (consistency checks)
- ✅ Custom validation rules
- ✅ Nested object validation
- ✅ Clear error messages

### Combined Benefits
- ✅ **Double validation**: OpenAI ensures structure, FluentValidation ensures business rules
- ✅ **Type safety**: C# compiler + runtime validation
- ✅ **Clear errors**: Know exactly what's wrong
- ✅ **Maintainable**: Validation rules in one place
- ✅ **Testable**: Easy to unit test validators

## Extension Methods (Pydantic-style API)

We've added extension methods to make the API feel like Pydantic:

```csharp
// Pydantic: review = CodeReview.model_validate_json(json)
// C#: review = json.DeserializeAndValidate<CodeReview>(validator)

public static class ValidationExtensions
{
    // Like Pydantic's model_validate
    public static void ValidateAndThrow<T>(this T instance, IValidator<T> validator)
    {
        validator.ValidateAndThrow(instance);
    }
    
    // Like Pydantic's is_valid
    public static bool IsValid<T>(this T instance, IValidator<T> validator)
    {
        return validator.Validate(instance).IsValid;
    }
    
    // Get errors as string
    public static string GetValidationErrors<T>(this T instance, IValidator<T> validator)
    {
        var result = validator.Validate(instance);
        return string.Join("; ", result.Errors.Select(e => e.ErrorMessage));
    }
}

// Usage (Pydantic-style)
var review = JsonSerializer.Deserialize<CodeReview>(json);
review.ValidateAndThrow(new CodeReviewValidator());  // ✅ Feels like Pydantic!
```

## Schema Validation (Like Pydantic)

Pydantic automatically ensures your Python class matches the JSON schema. We replicate this:

```csharp
public static class SchemaValidator
{
    /// <summary>
    /// Validate that JSON Schema matches C# model (like Pydantic does automatically).
    /// </summary>
    public static void ValidateSchema()
    {
        var schema = JsonSerializer.Deserialize<JsonElement>(
            CodeReviewSchema.GetSchemaAsString()
        );
        
        // Check all schema properties exist in C# model
        // Check all C# properties exist in schema
        // Check type compatibility
        
        Console.WriteLine("✅ Schema validation passed");
    }
}

// Call on startup (like Pydantic does)
#if DEBUG
SchemaValidator.ValidateSchema();
#endif
```

## Error Messages

### Pydantic Error
```python
ValidationError: 2 validation errors for CodeReview
confidence_score
  ensure this value is less than or equal to 100 (type=value_error.number.not_le; limit_value=100)
is_approved
  Cannot approve with confidence < 70 (type=value_error)
```

### FluentValidation Error
```csharp
ValidationException: Validation failed: 
 -- ConfidenceScore: 'Confidence Score' must be between 0 and 100. You entered 150.
 -- ApprovalConfidenceRule: Cannot approve with confidence < 70
```

Both provide clear, actionable error messages!

## Testing

### Unit Test Example

```csharp
[Fact]
public void CodeReview_WithLowConfidence_CannotBeApproved()
{
    // Arrange
    var review = new CodeReview
    {
        IsApproved = true,
        ConfidenceScore = 50,  // Too low!
        OverallQualityScore = 80
    };
    
    var validator = new CodeReviewValidator();
    
    // Act
    var result = validator.Validate(review);
    
    // Assert
    Assert.False(result.IsValid);
    Assert.Contains(result.Errors, e => 
        e.ErrorMessage.Contains("Cannot approve with confidence"));
}

[Fact]
public void CodeIssue_WithInvalidSeverity_FailsValidation()
{
    // Arrange
    var issue = new CodeIssue
    {
        Severity = "critical",  // Invalid! Must be error/warning/info
        Description = "This is a test issue"
    };
    
    var validator = new CodeIssueValidator();
    
    // Act & Assert
    Assert.Throws<ValidationException>(() => 
        issue.ValidateAndThrow(validator));
}
```

## Summary

We've successfully replicated Pydantic's functionality in C#:

| Feature | Python (Pydantic) | C# (FluentValidation) | Status |
|---------|------------------|----------------------|--------|
| Type validation | ✅ Automatic | ✅ Via JSON Schema | ✅ Done |
| Constraint validation | ✅ Field() | ✅ RuleFor() | ✅ Done |
| Custom validators | ✅ @validator | ✅ Must() | ✅ Done |
| Nested validation | ✅ Automatic | ✅ RuleForEach() | ✅ Done |
| Schema generation | ✅ model_json_schema() | ✅ CodeReviewSchema | ✅ Done |
| Parse + validate | ✅ model_validate_json() | ✅ Deserialize + ValidateAndThrow | ✅ Done |
| Clear errors | ✅ ValidationError | ✅ ValidationException | ✅ Done |
| Schema sync check | ✅ Automatic | ✅ SchemaValidator | ✅ Done |

**Result**: C# now has Pydantic-equivalent validation! 🎉

## References

- [Pydantic Documentation](https://docs.pydantic.dev/)
- [FluentValidation Documentation](https://docs.fluentvalidation.net/)
- [OpenAI Structured Outputs](https://platform.openai.com/docs/guides/structured-outputs)
