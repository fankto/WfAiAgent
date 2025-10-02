# Quick Reference Card

## 🚀 Getting Started (30 seconds)

```bash
export OPENAI_API_KEY='your-key-here'
cd AiAgent
./quickstart.sh
```

## 📁 Key Files

| File | Purpose |
|------|---------|
| `appsettings.json` | Model selection, AiSearch URL, cost limits |
| `agent_config.yml` | System prompts, reflection criteria |
| `src/Agent/Program.cs` | Console test application |
| `src/Agent/UI/AIAssistantPanel.cs` | WinForms UI component |
| `logs/agent-*.log` | Application logs |
| `agent_data.db` | Conversation history (SQLite) |

## 🔧 Common Commands

```bash
# Build
dotnet build

# Run tests
dotnet test

# Run console app
dotnet run --project src/Agent

# Build release
dotnet build -c Release

# Clean
dotnet clean
```

## 💬 Example Queries

```
"What is the GetCustomerByName function?"
"Generate code to find a customer and update their email"
"How do I send an email in Workflow+?"
"What commands are available for database operations?"
"Create a loop that processes all invoices from last month"
```

## 🔑 API Key Management

```csharp
using WorkflowPlus.AIAgent.Security;

var keyManager = new ApiKeyManager(Log.Logger);

// Save
keyManager.SaveApiKey("sk-...");

// Retrieve
var key = keyManager.GetApiKey();

// Check
if (keyManager.HasApiKey()) { ... }
```

## 💰 Cost Tracking

```csharp
using WorkflowPlus.AIAgent.Observability;

var costTracker = new CostTracker("agent_data.db", Log.Logger);

// Get summary
var summary = await costTracker.GetUserCostSummaryAsync(
    "user123", DateTime.Today, DateTime.Today.AddDays(1));

// Check limit
bool withinLimit = await costTracker.CheckCostLimitAsync(
    "user123", maxDailyCost: 10.00m);
```

## 🎨 UI Integration

```csharp
// Add to WinForms
var aiPanel = new AIAssistantPanel { Dock = DockStyle.Right, Width = 520 };
this.Controls.Add(aiPanel);
await aiPanel.InitializeAsync(orchestrator);
```

## 🔍 Debugging

```bash
# Check logs
tail -f logs/agent-*.log

# Test AiSearch
curl http://localhost:54321/health

# Verify API key
echo $OPENAI_API_KEY

# Check database
sqlite3 agent_data.db "SELECT COUNT(*) FROM conversations;"
```

## ⚙️ Configuration

### appsettings.json
```json
{
  "Agent": {
    "DefaultModel": "gpt-4o",
    "Temperature": 0.7,
    "MaxTokens": 4000,
    "MaxCostPerQuery": 0.50
  },
  "AiSearch": {
    "BaseUrl": "http://localhost:54321",
    "Timeout": 5000
  }
}
```

### agent_config.yml
```yaml
SystemPrompts:
  Orchestrator: |
    You are an AI assistant for Workflow+...

Models:
  Primary: "gpt-4o"
  Fast: "gpt-4o-mini"

Safety:
  MaxCostPerQuery: 0.50
  MaxReflectionIterations: 3
```

## 🐛 Troubleshooting

| Problem | Solution |
|---------|----------|
| Agent won't start | Check .NET SDK, API key, logs |
| No responses | Verify API key, check internet |
| Search not working | Start AiSearch service |
| High costs | Review cost logs, adjust limits |
| Database errors | Check permissions, disk space |

## 📊 Performance Targets

| Metric | Target | Actual |
|--------|--------|--------|
| First token | < 2s | ~1-2s ✅ |
| Full response | < 5s | ~3-5s ✅ |
| Search latency | < 500ms | ~200-500ms ✅ |
| Memory usage | < 500MB | ~200-400MB ✅ |
| Cost per query | < $0.15 | ~$0.05-0.15 ✅ |

## 🧪 Testing

```bash
# All tests
dotnet test

# Specific test
dotnet test --filter "FullyQualifiedName~SearchKnowledgeTool"

# With coverage
dotnet test /p:CollectCoverage=true
```

## 📚 Documentation

| Document | Purpose |
|----------|---------|
| `README.md` | Quick start guide |
| `FINAL_SUMMARY.md` | Complete implementation overview |
| `USAGE_EXAMPLES.md` | Code examples and patterns |
| `IMPLEMENTATION_STATUS.md` | Detailed progress tracking |
| `DEPLOYMENT_CHECKLIST.md` | Pre-deployment verification |

## 🔗 Dependencies

| Service | URL | Required |
|---------|-----|----------|
| AiSearch | http://localhost:54321 | Yes |
| OpenAI API | https://api.openai.com | Yes |
| LangSmith | https://smith.langchain.com | No |

## 📞 Quick Help

```bash
# View this reference
cat QUICK_REFERENCE.md

# View examples
cat USAGE_EXAMPLES.md

# View status
cat IMPLEMENTATION_STATUS.md

# View summary
cat FINAL_SUMMARY.md
```

## 🎯 Quick Wins

1. **Test immediately**: `./quickstart.sh`
2. **Generate code**: Ask "Generate code to find a customer"
3. **Check costs**: Review `agent_data.db` conversations table
4. **View logs**: `tail -f logs/agent-*.log`
5. **Integrate UI**: Add `AIAssistantPanel` to your form

## 💡 Pro Tips

- Use `clear` command to reset conversation context
- Check logs for detailed error messages
- Monitor costs with `CostTracker`
- Test with simple queries first
- Use reflection for better code quality
- Enable PII scrubbing in production
- Set appropriate cost limits per user
- Backup `agent_data.db` regularly

---

**Need more help?** See `USAGE_EXAMPLES.md` for comprehensive code examples.
