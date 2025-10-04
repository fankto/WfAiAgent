using FluentValidation;
using WorkflowPlus.AIAgent.Core.Models;

namespace WorkflowPlus.AIAgent.Core.Validation;

/// <summary>
/// Pydantic-equivalent validator for CodeReview model.
/// Ensures all constraints and business rules are enforced.
/// </summary>
public class CodeReviewValidator : AbstractValidator<CodeReview>
{
    public CodeReviewValidator()
    {
        // Required fields
        RuleFor(x => x.IsApproved)
            .NotNull()
            .WithMessage("IsApproved is required");

        // Confidence score constraints (like Pydantic's Field(ge=0, le=100))
        RuleFor(x => x.ConfidenceScore)
            .InclusiveBetween(0, 100)
            .WithMessage("ConfidenceScore must be between 0 and 100");

        // Overall quality score constraints
        RuleFor(x => x.OverallQualityScore)
            .InclusiveBetween(0, 100)
            .WithMessage("OverallQualityScore must be between 0 and 100");

        // Business rule: Cannot approve with low confidence (like Pydantic's @validator)
        RuleFor(x => x)
            .Must(x => !x.IsApproved || x.ConfidenceScore >= 70)
            .WithMessage("Cannot approve code with confidence score below 70")
            .WithName("ApprovalConfidenceRule");

        // Business rule: Cannot approve with critical issues
        RuleFor(x => x)
            .Must(x => !x.IsApproved || !x.HasCriticalIssues())
            .WithMessage("Cannot approve code with critical errors")
            .WithName("ApprovalCriticalIssuesRule");

        // Validate all issue lists are not null
        RuleFor(x => x.SyntaxErrors)
            .NotNull()
            .WithMessage("SyntaxErrors list cannot be null");

        RuleFor(x => x.LogicIssues)
            .NotNull()
            .WithMessage("LogicIssues list cannot be null");

        RuleFor(x => x.BestPracticeViolations)
            .NotNull()
            .WithMessage("BestPracticeViolations list cannot be null");

        RuleFor(x => x.SecurityConcerns)
            .NotNull()
            .WithMessage("SecurityConcerns list cannot be null");

        RuleFor(x => x.SuggestedImprovements)
            .NotNull()
            .WithMessage("SuggestedImprovements list cannot be null");

        // Validate each issue in the lists
        RuleForEach(x => x.SyntaxErrors)
            .SetValidator(new CodeIssueValidator());

        RuleForEach(x => x.LogicIssues)
            .SetValidator(new CodeIssueValidator());

        RuleForEach(x => x.BestPracticeViolations)
            .SetValidator(new CodeIssueValidator());

        RuleForEach(x => x.SecurityConcerns)
            .SetValidator(new CodeIssueValidator());

        // Business rule: High quality score should have few issues
        RuleFor(x => x)
            .Must(x => x.OverallQualityScore < 80 || x.TotalIssueCount() <= 3)
            .WithMessage("High quality score (>=80) should have 3 or fewer issues")
            .WithName("QualityScoreConsistencyRule");
    }
}

/// <summary>
/// Pydantic-equivalent validator for CodeIssue model.
/// </summary>
public class CodeIssueValidator : AbstractValidator<CodeIssue>
{
    public CodeIssueValidator()
    {
        // Severity must be one of the allowed values (like Pydantic's Literal)
        RuleFor(x => x.Severity)
            .NotEmpty()
            .Must(s => new[] { "error", "warning", "info" }.Contains(s))
            .WithMessage("Severity must be 'error', 'warning', or 'info'");

        // Description is required and must not be empty
        RuleFor(x => x.Description)
            .NotEmpty()
            .WithMessage("Description is required")
            .MinimumLength(10)
            .WithMessage("Description must be at least 10 characters")
            .MaximumLength(500)
            .WithMessage("Description must not exceed 500 characters");

        // Line number must be positive if provided
        RuleFor(x => x.Line)
            .GreaterThan(0)
            .When(x => x.Line.HasValue)
            .WithMessage("Line number must be greater than 0");

        // Suggested fix should be meaningful if provided
        RuleFor(x => x.SuggestedFix)
            .MinimumLength(5)
            .When(x => !string.IsNullOrEmpty(x.SuggestedFix))
            .WithMessage("SuggestedFix must be at least 5 characters if provided");
    }
}
