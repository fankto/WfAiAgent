using System.ComponentModel;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.SemanticKernel;
using Serilog;

namespace WorkflowPlus.AIAgent.Tools;

/// <summary>
/// Tool for validating Workflow+ script syntax using Roslyn.
/// </summary>
public class ScriptValidationTool
{
    private readonly ILogger _logger;

    public ScriptValidationTool()
    {
        _logger = Log.ForContext<ScriptValidationTool>();
    }

    [KernelFunction("validate_script")]
    [Description("Validate Workflow+ script code for syntax errors. Use this before presenting code to the user.")]
    public async Task<string> ValidateSyntaxAsync(
        [Description("The script code to validate")] string scriptCode)
    {
        _logger.Information("Validating script syntax");

        try
        {
            // Parse the code using Roslyn
            var syntaxTree = CSharpSyntaxTree.ParseText(scriptCode);
            var diagnostics = syntaxTree.GetDiagnostics();

            var errors = diagnostics
                .Where(d => d.Severity == DiagnosticSeverity.Error)
                .ToList();

            if (!errors.Any())
            {
                _logger.Information("Script validation passed");
                return "✓ Syntax validation passed. No errors found.";
            }

            // Format errors
            var sb = new StringBuilder();
            sb.AppendLine($"✗ Found {errors.Count} syntax error(s):\n");

            foreach (var error in errors)
            {
                var lineSpan = error.Location.GetLineSpan();
                var line = lineSpan.StartLinePosition.Line + 1;
                var column = lineSpan.StartLinePosition.Character + 1;
                
                sb.AppendLine($"Line {line}, Column {column}: {error.GetMessage()}");
            }

            _logger.Warning("Script validation found {Count} errors", errors.Count);
            return sb.ToString();
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Error validating script");
            return "Error during validation. Please check the code manually.";
        }
    }

    [KernelFunction("check_license_requirements")]
    [Description("Check if code uses commands that require specific license tiers")]
    public async Task<string> CheckLicenseRequirementsAsync(
        [Description("The script code to check")] string scriptCode)
    {
        _logger.Information("Checking license requirements");

        // Simple pattern matching for known premium commands
        // In a real implementation, this would query the documentation database
        var premiumPatterns = new Dictionary<string, string>
        {
            { "GenericAi.", "Enterprise + AI Extensions Pack" },
            { "SqlConnect.", "Professional + SQL Plugin" },
            { "CloudSync.", "Enterprise + Cloud Pack" }
        };

        var requirements = new List<string>();

        foreach (var pattern in premiumPatterns)
        {
            if (scriptCode.Contains(pattern.Key))
            {
                requirements.Add($"- {pattern.Key}* commands require: {pattern.Value}");
            }
        }

        if (!requirements.Any())
        {
            return "✓ All commands are available in the Core license tier.";
        }

        var sb = new StringBuilder();
        sb.AppendLine("⚠ License Requirements:");
        sb.AppendLine();
        foreach (var req in requirements)
        {
            sb.AppendLine(req);
        }

        return sb.ToString();
    }
}
