#!/bin/bash

# Installation Verification Script
# Checks that all components are properly installed and configured

echo "╔════════════════════════════════════════════════════════════╗"
echo "║     AI Agent Installation Verification                    ║"
echo "╚════════════════════════════════════════════════════════════╝"
echo ""

ERRORS=0
WARNINGS=0

# Function to print status
print_status() {
    if [ $1 -eq 0 ]; then
        echo "✓ $2"
    else
        echo "✗ $2"
        ((ERRORS++))
    fi
}

print_warning() {
    echo "⚠ $1"
    ((WARNINGS++))
}

# Check .NET SDK
echo "Checking prerequisites..."
if command -v dotnet &> /dev/null; then
    VERSION=$(dotnet --version)
    print_status 0 ".NET SDK installed (version $VERSION)"
    
    # Check if version is 9.0 or higher
    MAJOR=$(echo $VERSION | cut -d. -f1)
    if [ "$MAJOR" -lt 9 ]; then
        print_warning ".NET SDK version should be 9.0 or higher"
    fi
else
    print_status 1 ".NET SDK not found"
fi

# Check for required files
echo ""
echo "Checking project files..."

FILES=(
    "WorkflowPlus.AIAgent.sln"
    "appsettings.json"
    "agent_config.yml"
    "src/Core/WorkflowPlus.AIAgent.Core.csproj"
    "src/Agent/WorkflowPlus.AIAgent.csproj"
    "src/Observability/WorkflowPlus.AIAgent.Observability.csproj"
    "tests/WorkflowPlus.AIAgent.Tests/WorkflowPlus.AIAgent.Tests.csproj"
)

for file in "${FILES[@]}"; do
    if [ -f "$file" ]; then
        print_status 0 "Found $file"
    else
        print_status 1 "Missing $file"
    fi
done

# Check for key source files
echo ""
echo "Checking source files..."

SOURCE_FILES=(
    "src/Agent/Orchestration/AgentOrchestrator.cs"
    "src/Agent/Tools/SearchKnowledgeTool.cs"
    "src/Agent/Tools/ScriptValidationTool.cs"
    "src/Agent/Memory/ConversationManager.cs"
    "src/Agent/Security/ApiKeyManager.cs"
    "src/Agent/UI/AIAssistantPanel.cs"
    "src/Observability/CostTracker.cs"
)

for file in "${SOURCE_FILES[@]}"; do
    if [ -f "$file" ]; then
        print_status 0 "Found $file"
    else
        print_status 1 "Missing $file"
    fi
done

# Check API key
echo ""
echo "Checking configuration..."

if [ -n "$OPENAI_API_KEY" ]; then
    print_status 0 "OPENAI_API_KEY environment variable set"
else
    print_warning "OPENAI_API_KEY not set (required for operation)"
fi

# Check AiSearch service
echo ""
echo "Checking external services..."

if curl -s http://localhost:54321/health > /dev/null 2>&1; then
    print_status 0 "AiSearch service is running"
else
    print_warning "AiSearch service not detected (required for operation)"
fi

# Try to restore packages
echo ""
echo "Checking dependencies..."

if dotnet restore > /dev/null 2>&1; then
    print_status 0 "NuGet packages restored successfully"
else
    print_status 1 "Failed to restore NuGet packages"
fi

# Try to build
echo ""
echo "Checking build..."

if dotnet build --no-restore > /dev/null 2>&1; then
    print_status 0 "Project builds successfully"
else
    print_status 1 "Build failed"
fi

# Check if tests can run
echo ""
echo "Checking tests..."

if dotnet test --no-build --verbosity quiet > /dev/null 2>&1; then
    print_status 0 "Tests run successfully"
else
    print_warning "Some tests failed (may require API key or AiSearch)"
fi

# Check documentation
echo ""
echo "Checking documentation..."

DOCS=(
    "README.md"
    "FINAL_SUMMARY.md"
    "USAGE_EXAMPLES.md"
    "IMPLEMENTATION_STATUS.md"
    "DEPLOYMENT_CHECKLIST.md"
    "QUICK_REFERENCE.md"
)

for doc in "${DOCS[@]}"; do
    if [ -f "$doc" ]; then
        print_status 0 "Found $doc"
    else
        print_status 1 "Missing $doc"
    fi
done

# Summary
echo ""
echo "╔════════════════════════════════════════════════════════════╗"
echo "║     Verification Summary                                   ║"
echo "╚════════════════════════════════════════════════════════════╝"
echo ""

if [ $ERRORS -eq 0 ] && [ $WARNINGS -eq 0 ]; then
    echo "✓ All checks passed! Installation is complete."
    echo ""
    echo "Next steps:"
    echo "  1. Set OPENAI_API_KEY if not already set"
    echo "  2. Start AiSearch service if not running"
    echo "  3. Run: ./quickstart.sh"
    exit 0
elif [ $ERRORS -eq 0 ]; then
    echo "⚠ Installation complete with $WARNINGS warning(s)"
    echo ""
    echo "The agent should work, but some features may be limited."
    echo "Review warnings above and address if needed."
    exit 0
else
    echo "✗ Installation incomplete: $ERRORS error(s), $WARNINGS warning(s)"
    echo ""
    echo "Please address the errors above before proceeding."
    exit 1
fi
