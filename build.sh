#!/bin/bash

# Build and test script for AI Agent

set -e

echo "=== Building Workflow+ AI Agent ==="
echo ""

# Check for .NET SDK
if ! command -v dotnet &> /dev/null; then
    echo "ERROR: .NET SDK not found. Please install .NET 9.0 SDK."
    exit 1
fi

echo "✓ .NET SDK found: $(dotnet --version)"
echo ""

# Restore dependencies
echo "Restoring NuGet packages..."
dotnet restore
echo "✓ Packages restored"
echo ""

# Build solution
echo "Building solution..."
dotnet build --configuration Release --no-restore
echo "✓ Build successful"
echo ""

# Run tests
echo "Running tests..."
dotnet test --configuration Release --no-build --verbosity normal
echo "✓ Tests completed"
echo ""

echo "=== Build Complete ==="
echo ""
echo "To run the agent:"
echo "  1. Set your OpenAI API key: export OPENAI_API_KEY='your-key'"
echo "  2. Start AiSearch service: cd ../AiSearch/src/Service && dotnet run"
echo "  3. Run agent: dotnet run --project src/Agent"
echo ""
