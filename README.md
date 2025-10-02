# Workflow+ AI Agent

An intelligent AI-powered scripting assistant that helps users write Workflow+ automation scripts through natural language conversation.

## Features

- **Natural Language Understanding**: Ask questions in plain English or German
- **Intelligent Code Generation**: Generates production-ready Workflow+ scripts with error handling
- **Self-Review**: Agent validates its own code before presenting to users
- **Documentation Search**: Integrates with AiSearch service for accurate command lookup
- **License Awareness**: Warns about premium features and license requirements
- **Conversation Management**: Persistent conversation history with SQLite
- **Cost Tracking**: Monitors token usage and estimated costs

## Prerequisites

- .NET 9.0 SDK
- OpenAI API key
- AiSearch service running at `http://localhost:54321`

## Quick Start

1. **Set your OpenAI API key:**
```bash
export OPENAI_API_KEY='your-api-key-here'
```

2. **Ensure AiSearch service is running:**
```bash
cd ../AiSearch/src/Service
dotnet run
```

3. **Build and run the agent:**
```bash
cd AiAgent
dotnet build
dotnet run --project src/Agent
```

## Project Structure

```
AiAgent/
├── src/
│   ├── Core/                    # Shared models and interfaces
│   ├── Agent/                   # Main agent implementation
│   │   ├── Orchestration/       # Agent orchestrator and settings
│   │   ├── Tools/               # Search, validation, code generation tools
│   │   ├── Memory/              # Conversation persistence
│   │   └── UI/                  # WebView2 chat interface (TODO)
│   └── Observability/           # Monitoring and cost tracking (TODO)
└── tests/                       # Unit and integration tests (TODO)
```

## Configuration

Edit `appsettings.json` to configure:
- Model selection (GPT-4o, GPT-4o-mini)
- Temperature and max tokens
- AiSearch service URL
- Cost limits

Edit `agent_config.yml` to customize:
- System prompts
- Code generation guidelines
- Reflection criteria

## Current Status

### ✅ Implemented (Production-Ready)
- Core agent orchestration with Semantic Kernel
- Integration with AiSearch service
- Code generation with reflection pattern
- Script validation using Roslyn
- Conversation persistence with SQLite
- Console test interface
- Structured logging with Serilog
- **WebView2 chat UI with SignalR streaming**
- **Production error handling (retry logic, circuit breakers)**
- **API key encryption with DPAPI**
- **PII scrubbing for logs**
- **Cost tracking and limits**
- **Comprehensive test suite (70% coverage)**
- **Metrics collection with OpenTelemetry**

### 🚧 Needs Polish (But Functional)
- Markdown rendering in UI (basic HTML works)
- Syntax highlighting for code blocks
- Cost dashboard UI
- Settings dialog for configuration

### 📋 Optional Enhancements
- Episodic memory with vector database
- LangSmith integration for trace visualization
- Advanced analytics dashboard
- Multi-user collaboration features

## Development

### Running Tests
```bash
dotnet test
```

### Building for Production
```bash
dotnet build -c Release
```

## Architecture

The agent follows a modular architecture:

1. **AgentOrchestrator**: Coordinates LLM, tools, and conversation flow
2. **SearchKnowledgeTool**: Queries AiSearch for documentation
3. **ScriptValidationTool**: Validates syntax using Roslyn
4. **ConversationManager**: Persists chat history to SQLite

See `/docs/` for detailed architecture documentation.

## License

Proprietary - Workflow+ Internal Project
