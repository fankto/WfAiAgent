using FluentAssertions;
using WorkflowPlus.AIAgent.Tools;
using Xunit;

namespace WorkflowPlus.AIAgent.Tests.Unit;

public class ScriptValidationToolTests
{
    [Fact]
    public async Task ValidateSyntaxAsync_WithValidCode_ReturnsSuccess()
    {
        // Arrange
        var tool = new ScriptValidationTool();
        var validCode = @"
            var customerName = ""Acme Corp"";
            if (GetCustomerByName(customerName)) {
                Log(""Customer found"");
            }
        ";

        // Act
        var result = await tool.ValidateSyntaxAsync(validCode);

        // Assert
        result.Should().Contain("✓");
        result.Should().Contain("passed");
    }

    [Fact]
    public async Task ValidateSyntaxAsync_WithInvalidCode_ReturnsErrors()
    {
        // Arrange
        var tool = new ScriptValidationTool();
        var invalidCode = @"
            var customerName = ""Acme Corp""
            if (GetCustomerByName(customerName) {
                Log(""Customer found"")
            }
        ";

        // Act
        var result = await tool.ValidateSyntaxAsync(invalidCode);

        // Assert
        result.Should().Contain("✗");
        result.Should().Contain("error");
    }

    [Fact]
    public async Task CheckLicenseRequirementsAsync_WithCoreCommands_ReturnsNoRequirements()
    {
        // Arrange
        var tool = new ScriptValidationTool();
        var code = @"
            if (GetCustomerByName(""Test"")) {
                Log(""Found"");
            }
        ";

        // Act
        var result = await tool.CheckLicenseRequirementsAsync(code);

        // Assert
        result.Should().Contain("Core license");
    }

    [Fact]
    public async Task CheckLicenseRequirementsAsync_WithPremiumCommands_ReturnsRequirements()
    {
        // Arrange
        var tool = new ScriptValidationTool();
        var code = @"
            GenericAi.AIChat_Initialize(""config"");
            SqlConnect.OpenConnection(""db"");
        ";

        // Act
        var result = await tool.CheckLicenseRequirementsAsync(code);

        // Assert
        result.Should().Contain("License Requirements");
        result.Should().Contain("Enterprise");
    }
}
