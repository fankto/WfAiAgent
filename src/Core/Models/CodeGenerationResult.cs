namespace WorkflowPlus.AIAgent.Core.Models;

/// <summary>
/// Represents the result of code generation with reflection.
/// </summary>
public class CodeGenerationResult
{
    public string Code { get; set; } = string.Empty;
    public bool IsValid { get; set; }
    public List<string> ValidationErrors { get; set; } = new();
    public List<string> Warnings { get; set; } = new();
    public int ReflectionIterations { get; set; }
    public List<string> LicenseRequirements { get; set; } = new();
    public string? Explanation { get; set; }

    public static CodeGenerationResult Success(string code, int iterations)
    {
        return new CodeGenerationResult
        {
            Code = code,
            IsValid = true,
            ReflectionIterations = iterations
        };
    }

    public static CodeGenerationResult WithWarning(string code, string warning)
    {
        return new CodeGenerationResult
        {
            Code = code,
            IsValid = true,
            Warnings = new List<string> { warning }
        };
    }

    public static CodeGenerationResult Failure(List<string> errors)
    {
        return new CodeGenerationResult
        {
            IsValid = false,
            ValidationErrors = errors
        };
    }
}
