# AI Agent Usage Examples

This document provides practical examples of using the Workflow+ AI Agent.

## Table of Contents
1. [Console Application](#console-application)
2. [WinForms Integration](#winforms-integration)
3. [API Key Management](#api-key-management)
4. [Error Handling](#error-handling)
5. [Cost Tracking](#cost-tracking)

---

## Console Application

### Basic Usage

```csharp
using Serilog;
using WorkflowPlus.AIAgent.Orchestration;

// Configure logging
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Debug()
    .WriteTo.Console()
    .CreateLogger();

// Load settings
var settings = AgentSettings.LoadFromYaml("agent_config.yml");

// Get API key (from environment or secure storage)
var apiKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY");

// Create orchestrator
var orchestrator = new AgentOrchestrator(apiKey, settings, Log.Logger);

// Process a query
var response = await orchestrator.ProcessQueryAsync(
    "How do I find a customer by name and update their email?"
);

Console.WriteLine($"Agent: {response.Content}");
Console.WriteLine($"Tokens: {response.TokensUsed}, Cost: ${response.EstimatedCost:F4}");
```

### Streaming Responses

```csharp
Console.Write("Agent: ");

await foreach (var token in orchestrator.StreamResponseAsync(userQuery))
{
    Console.Write(token);
}

Console.WriteLine();
```

### Code Generation with Reflection

```csharp
var result = await orchestrator.GenerateCodeWithReflectionAsync(
    "Find customer 'Acme Corp' and update email to support@acme.com"
);

if (result.IsValid)
{
    Console.WriteLine("Generated Code:");
    Console.WriteLine(result.Code);
    Console.WriteLine($"\nReflection iterations: {result.ReflectionIterations}");
}
else
{
    Console.WriteLine("Validation errors:");
    foreach (var error in result.ValidationErrors)
    {
        Console.WriteLine($"  - {error}");
    }
}
```

---

## WinForms Integration

### Adding AI Assistant Panel to Form

```csharp
using WorkflowPlus.AIAgent.UI;
using WorkflowPlus.AIAgent.Orchestration;

public partial class MainForm : Form
{
    private AIAssistantPanel? _aiPanel;
    private AgentOrchestrator? _orchestrator;

    public MainForm()
    {
        InitializeComponent();
        InitializeAIAssistantAsync();
    }

    private async void InitializeAIAssistantAsync()
    {
        try
        {
            // Load settings
            var settings = AgentSettings.LoadFromYaml("agent_config.yml");
            
            // Get API key
            var apiKey = GetApiKey(); // Your method to retrieve key
            
            // Create orchestrator
            _orchestrator = new AgentOrchestrator(apiKey, settings, Log.Logger);
            
            // Create and dock AI panel
            _aiPanel = new AIAssistantPanel
            {
                Dock = DockStyle.Right,
                Width = 520
            };
            
            this.Controls.Add(_aiPanel);
            
            // Initialize with SignalR
            await _aiPanel.InitializeAsync(_orchestrator);
            
            MessageBox.Show("AI Assistant ready!", "Success", 
                MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Failed to initialize AI Assistant: {ex.Message}", 
                "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }
}
```

### Toggle AI Panel Visibility

```csharp
private void ToggleAIAssistant_Click(object sender, EventArgs e)
{
    if (_aiPanel != null)
    {
        _aiPanel.Visible = !_aiPanel.Visible;
    }
}
```

---

## API Key Management

### Secure Storage with DPAPI

```csharp
using WorkflowPlus.AIAgent.Security;

var keyManager = new ApiKeyManager(Log.Logger);

// Save API key securely
keyManager.SaveApiKey("sk-your-api-key-here");

// Retrieve API key
var apiKey = keyManager.GetApiKey();

// Check if key exists
if (keyManager.HasApiKey())
{
    Console.WriteLine($"Stored key: {keyManager.GetMaskedApiKey()}");
}

// Delete key
keyManager.DeleteApiKey();
```

### API Key Configuration Dialog

```csharp
public class ApiKeyDialog : Form
{
    private TextBox _keyTextBox;
    private Button _saveButton;
    private ApiKeyManager _keyManager;

    public ApiKeyDialog()
    {
        _keyManager = new ApiKeyManager(Log.Logger);
        InitializeComponents();
        LoadExistingKey();
    }

    private void LoadExistingKey()
    {
        if (_keyManager.HasApiKey())
        {
            _keyTextBox.Text = _keyManager.GetMaskedApiKey();
            _keyTextBox.UseSystemPasswordChar = true;
        }
    }

    private void SaveButton_Click(object sender, EventArgs e)
    {
        try
        {
            var apiKey = _keyTextBox.Text.Trim();
            
            if (string.IsNullOrEmpty(apiKey))
            {
                MessageBox.Show("Please enter an API key", "Error");
                return;
            }

            _keyManager.SaveApiKey(apiKey);
            MessageBox.Show("API key saved securely", "Success");
            this.DialogResult = DialogResult.OK;
            this.Close();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Failed to save API key: {ex.Message}", "Error");
        }
    }
}
```

---

## Error Handling

### Retry Logic for Transient Failures

```csharp
using WorkflowPlus.AIAgent.Orchestration;

var retryPolicy = new RetryPolicy(Log.Logger, maxRetries: 3, baseDelayMs: 1000);

var response = await retryPolicy.ExecuteAsync(
    async () => await orchestrator.ProcessQueryAsync(query),
    operationName: "ProcessQuery"
);
```

### Circuit Breaker for External Services

```csharp
var circuitBreaker = new CircuitBreaker(Log.Logger, failureThreshold: 5, timeoutSeconds: 60);

try
{
    var response = await circuitBreaker.ExecuteAsync(
        async () => await orchestrator.ProcessQueryAsync(query),
        serviceName: "OpenAI"
    );
}
catch (InvalidOperationException ex) when (ex.Message.Contains("Circuit breaker is open"))
{
    Console.WriteLine("OpenAI service is temporarily unavailable. Please try again later.");
}
```

### Graceful Degradation

```csharp
try
{
    var response = await orchestrator.ProcessQueryAsync(query);
    return response;
}
catch (HttpRequestException ex)
{
    Log.Warning("OpenAI API unavailable, falling back to search-only mode");
    
    // Fallback: Just search documentation without LLM
    var searchTool = new SearchKnowledgeTool();
    var searchResults = await searchTool.SearchCommandsAsync(query);
    
    return new AgentResponse
    {
        Content = $"I couldn't reach the AI service, but here's what I found in the documentation:\n\n{searchResults}",
        Success = true
    };
}
```

---

## Cost Tracking

### Monitor User Costs

```csharp
using WorkflowPlus.AIAgent.Observability;

var costTracker = new CostTracker("agent_data.db", Log.Logger);

// Get cost summary for a user
var summary = await costTracker.GetUserCostSummaryAsync(
    userId: "user123",
    startDate: DateTime.UtcNow.Date,
    endDate: DateTime.UtcNow.Date.AddDays(1)
);

Console.WriteLine($"Total Queries: {summary.TotalQueries}");
Console.WriteLine($"Total Tokens: {summary.TotalTokens}");
Console.WriteLine($"Total Cost: ${summary.TotalCostUSD:F2}");
Console.WriteLine($"Avg Cost/Query: ${summary.AvgCostPerQuery:F4}");
```

### Check Cost Limits

```csharp
var maxDailyCost = 10.00m; // $10 per day limit

if (!await costTracker.CheckCostLimitAsync("user123", maxDailyCost))
{
    MessageBox.Show(
        "You have reached your daily cost limit. Please try again tomorrow.",
        "Cost Limit Reached",
        MessageBoxButtons.OK,
        MessageBoxIcon.Warning
    );
    return;
}

// Proceed with query
var response = await orchestrator.ProcessQueryAsync(query);
```

### Daily Cost Report

```csharp
var dailyCosts = await costTracker.GetDailyCostsAsync("user123", days: 30);

Console.WriteLine("Daily Cost Report (Last 30 Days):");
Console.WriteLine("Date       | Queries | Tokens  | Cost");
Console.WriteLine("-----------|---------|---------|--------");

foreach (var day in dailyCosts)
{
    Console.WriteLine($"{day.Date} | {day.QueryCount,7} | {day.TotalTokens,7} | ${day.TotalCost,6:F2}");
}
```

---

## Conversation Management

### Save and Load Conversations

```csharp
using WorkflowPlus.AIAgent.Memory;
using WorkflowPlus.AIAgent.Core.Models;

var convManager = new ConversationManager("agent_data.db", Log.Logger);

// Create new conversation
var conversationId = await convManager.CreateConversationAsync(
    userId: "user123",
    title: "Customer Management Help"
);

// Save messages
await convManager.SaveMessageAsync(new ConversationMessage
{
    ConversationId = conversationId,
    Role = "user",
    Content = "How do I find a customer?"
});

await convManager.SaveMessageAsync(new ConversationMessage
{
    ConversationId = conversationId,
    Role = "assistant",
    Content = "You can use the GetCustomerByName function..."
});

// Load conversation history
var history = await convManager.GetConversationHistoryAsync(conversationId);

foreach (var message in history)
{
    Console.WriteLine($"{message.Role}: {message.Content}");
}
```

### Search Conversations

```csharp
var matchingConversations = await convManager.SearchConversationsAsync(
    userId: "user123",
    searchTerm: "customer email"
);

Console.WriteLine($"Found {matchingConversations.Count} matching conversations");
```

---

## PII Scrubbing

### Scrub Sensitive Data Before Logging

```csharp
using WorkflowPlus.AIAgent.Security;

var userInput = "Contact John at john.doe@example.com or call 555-123-4567";

// Check if contains PII
if (PiiScrubber.ContainsPii(userInput))
{
    var scrubbed = PiiScrubber.Scrub(userInput);
    Log.Information("User query (scrubbed): {Query}", scrubbed);
    // Output: "Contact John at [EMAIL] or call [PHONE]"
}
else
{
    Log.Information("User query: {Query}", userInput);
}
```

---

## Metrics Collection

### Track Performance Metrics

```csharp
using WorkflowPlus.AIAgent.Observability;

var metrics = new MetricsCollector();

var stopwatch = System.Diagnostics.Stopwatch.StartNew();

try
{
    var response = await orchestrator.ProcessQueryAsync(query);
    
    stopwatch.Stop();
    
    // Record metrics
    metrics.RecordQuery(userId: "user123", model: "gpt-4o");
    metrics.RecordTokens(response.TokensUsed, model: "gpt-4o");
    metrics.RecordQueryDuration(stopwatch.ElapsedMilliseconds, model: "gpt-4o", success: true);
    metrics.RecordQueryCost(response.EstimatedCost, model: "gpt-4o");
}
catch (Exception ex)
{
    stopwatch.Stop();
    metrics.RecordError(ex.GetType().Name, "ProcessQuery");
    metrics.RecordQueryDuration(stopwatch.ElapsedMilliseconds, model: "gpt-4o", success: false);
    throw;
}
```

---

## Complete Example: Production-Ready Agent

```csharp
using Serilog;
using WorkflowPlus.AIAgent.Orchestration;
using WorkflowPlus.AIAgent.Security;
using WorkflowPlus.AIAgent.Observability;
using WorkflowPlus.AIAgent.Memory;

public class ProductionAgent
{
    private readonly AgentOrchestrator _orchestrator;
    private readonly ApiKeyManager _keyManager;
    private readonly CostTracker _costTracker;
    private readonly MetricsCollector _metrics;
    private readonly ConversationManager _convManager;
    private readonly RetryPolicy _retryPolicy;
    private readonly CircuitBreaker _circuitBreaker;

    public ProductionAgent()
    {
        // Initialize logging
        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Information()
            .WriteTo.Console()
            .WriteTo.File("logs/agent-.log", rollingInterval: RollingInterval.Day)
            .CreateLogger();

        // Initialize components
        _keyManager = new ApiKeyManager(Log.Logger);
        _costTracker = new CostTracker("agent_data.db", Log.Logger);
        _metrics = new MetricsCollector();
        _convManager = new ConversationManager("agent_data.db", Log.Logger);
        _retryPolicy = new RetryPolicy(Log.Logger);
        _circuitBreaker = new CircuitBreaker(Log.Logger);

        // Load settings and create orchestrator
        var settings = AgentSettings.LoadFromYaml("agent_config.yml");
        var apiKey = _keyManager.GetApiKey() ?? 
                     Environment.GetEnvironmentVariable("OPENAI_API_KEY") ??
                     throw new InvalidOperationException("No API key configured");

        _orchestrator = new AgentOrchestrator(apiKey, settings, Log.Logger);
    }

    public async Task<AgentResponse> ProcessQueryAsync(string userId, string query, string? conversationId = null)
    {
        // Check cost limit
        if (!await _costTracker.CheckCostLimitAsync(userId, maxDailyCost: 10.00m))
        {
            return new AgentResponse
            {
                Success = false,
                ErrorMessage = "Daily cost limit reached"
            };
        }

        // Scrub PII from logs
        var scrubbedQuery = PiiScrubber.Scrub(query);
        Log.Information("Processing query for user {UserId}: {Query}", userId, scrubbedQuery);

        var stopwatch = System.Diagnostics.Stopwatch.StartNew();

        try
        {
            // Execute with retry and circuit breaker
            var response = await _retryPolicy.ExecuteAsync(
                async () => await _circuitBreaker.ExecuteAsync(
                    async () => await _orchestrator.ProcessQueryAsync(query, conversationId),
                    serviceName: "OpenAI"
                ),
                operationName: "ProcessQuery"
            );

            stopwatch.Stop();

            // Record metrics
            _metrics.RecordQuery(userId, response.ModelUsed);
            _metrics.RecordTokens(response.TokensUsed, response.ModelUsed);
            _metrics.RecordQueryDuration(stopwatch.ElapsedMilliseconds, response.ModelUsed, success: true);
            _metrics.RecordQueryCost(response.EstimatedCost, response.ModelUsed);

            // Save to conversation history
            if (!string.IsNullOrEmpty(conversationId))
            {
                await _convManager.SaveMessageAsync(new ConversationMessage
                {
                    ConversationId = conversationId,
                    Role = "user",
                    Content = query
                });

                await _convManager.SaveMessageAsync(new ConversationMessage
                {
                    ConversationId = conversationId,
                    Role = "assistant",
                    Content = response.Content
                });

                await _convManager.UpdateConversationCostAsync(
                    conversationId, 
                    response.TokensUsed, 
                    response.EstimatedCost
                );
            }

            return response;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _metrics.RecordError(ex.GetType().Name, "ProcessQuery");
            _metrics.RecordQueryDuration(stopwatch.ElapsedMilliseconds, "unknown", success: false);
            
            Log.Error(ex, "Error processing query for user {UserId}", userId);
            
            return new AgentResponse
            {
                Success = false,
                ErrorMessage = "An error occurred processing your request"
            };
        }
    }
}
```

---

## Testing

### Unit Test Example

```csharp
[Fact]
public async Task ProcessQueryAsync_WithValidQuery_ReturnsResponse()
{
    // Arrange
    var mockOrchestrator = new Mock<AgentOrchestrator>();
    mockOrchestrator
        .Setup(o => o.ProcessQueryAsync(It.IsAny<string>(), It.IsAny<string>()))
        .ReturnsAsync(new AgentResponse
        {
            Content = "Test response",
            Success = true,
            TokensUsed = 100,
            EstimatedCost = 0.01m
        });

    // Act
    var response = await mockOrchestrator.Object.ProcessQueryAsync("test query");

    // Assert
    response.Should().NotBeNull();
    response.Success.Should().BeTrue();
    response.Content.Should().Be("Test response");
}
```

---

For more examples, see the test projects in `/tests/`.
