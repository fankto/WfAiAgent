using FluentValidation;
using FluentValidation.Results;

namespace WorkflowPlus.AIAgent.Core.Validation;

/// <summary>
/// Extension methods for validation (Pydantic-style).
/// </summary>
public static class ValidationExtensions
{
    /// <summary>
    /// Validate and throw if invalid (like Pydantic's model_validate).
    /// </summary>
    public static void ValidateAndThrow<T>(this T instance, IValidator<T> validator)
    {
        validator.ValidateAndThrow(instance);
    }

    /// <summary>
    /// Validate and return result (like Pydantic's model_validate with return_errors=True).
    /// </summary>
    public static ValidationResult ValidateWithResult<T>(this T instance, IValidator<T> validator)
    {
        return validator.Validate(instance);
    }

    /// <summary>
    /// Check if instance is valid (like Pydantic's is_valid).
    /// </summary>
    public static bool IsValid<T>(this T instance, IValidator<T> validator)
    {
        var result = validator.Validate(instance);
        return result.IsValid;
    }

    /// <summary>
    /// Get validation errors as formatted string.
    /// </summary>
    public static string GetValidationErrors<T>(this T instance, IValidator<T> validator)
    {
        var result = validator.Validate(instance);
        if (result.IsValid)
            return string.Empty;

        return string.Join("; ", result.Errors.Select(e => $"{e.PropertyName}: {e.ErrorMessage}"));
    }
}
