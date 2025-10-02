#!/bin/bash

# Quick Start Script for Workflow+ AI Agent
# This script helps you get up and running quickly

set -e

echo "╔════════════════════════════════════════════════════════════╗"
echo "║     Workflow+ AI Agent - Quick Start                      ║"
echo "╚════════════════════════════════════════════════════════════╝"
echo ""

# Check for .NET SDK
if ! command -v dotnet &> /dev/null; then
    echo "❌ ERROR: .NET SDK not found"
    echo "   Please install .NET 9.0 SDK from: https://dotnet.microsoft.com/download"
    exit 1
fi

echo "✓ .NET SDK found: $(dotnet --version)"
echo ""

# Check for OpenAI API key
if [ -z "$OPENAI_API_KEY" ]; then
    echo "⚠️  WARNING: OPENAI_API_KEY environment variable not set"
    echo ""
    read -p "Enter your OpenAI API key (or press Enter to skip): " api_key
    
    if [ -n "$api_key" ]; then
        export OPENAI_API_KEY="$api_key"
        echo "✓ API key set for this session"
    else
        echo "⚠️  Skipping API key setup. You'll need to set it later."
    fi
    echo ""
else
    echo "✓ OpenAI API key found"
    echo ""
fi

# Check if AiSearch is running
echo "Checking if AiSearch service is running..."
if curl -s http://localhost:54321/health > /dev/null 2>&1; then
    echo "✓ AiSearch service is running"
else
    echo "⚠️  WARNING: AiSearch service not detected at localhost:54321"
    echo ""
    echo "The AI Agent requires AiSearch to be running."
    echo "To start AiSearch, open a new terminal and run:"
    echo "  cd ../AiSearch/src/Service && dotnet run"
    echo ""
    read -p "Press Enter to continue anyway, or Ctrl+C to exit..."
fi
echo ""

# Build the project
echo "Building AI Agent..."
dotnet restore
dotnet build --configuration Release --no-restore

if [ $? -ne 0 ]; then
    echo "❌ Build failed. Please check the errors above."
    exit 1
fi

echo "✓ Build successful"
echo ""

# Run tests (optional)
read -p "Run tests before starting? (y/N): " run_tests
if [[ $run_tests =~ ^[Yy]$ ]]; then
    echo ""
    echo "Running tests..."
    dotnet test --configuration Release --no-build --verbosity normal
    echo ""
fi

# Start the agent
echo "╔════════════════════════════════════════════════════════════╗"
echo "║     Starting AI Agent Console                             ║"
echo "╚════════════════════════════════════════════════════════════╝"
echo ""
echo "Commands:"
echo "  - Type your questions naturally"
echo "  - Type 'clear' to reset the conversation"
echo "  - Type 'exit' to quit"
echo ""
echo "Example queries:"
echo "  - What is the GetCustomerByName function?"
echo "  - Generate code to find a customer and update their email"
echo "  - How do I send an email in Workflow+?"
echo ""
echo "Starting in 3 seconds..."
sleep 3
echo ""

dotnet run --project src/Agent --configuration Release --no-build
