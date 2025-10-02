using System.Windows.Forms;
using Microsoft.Web.WebView2.WinForms;
using Microsoft.Web.WebView2.Core;
using Microsoft.AspNetCore.SignalR.Client;
using Serilog;
using WorkflowPlus.AIAgent.Orchestration;
using System.Text.Json;
using System.Reflection;

namespace WorkflowPlus.AIAgent.UI;

/// <summary>
/// WinForms UserControl hosting the AI Agent chat interface.
/// Uses WebView2 for modern UI and SignalR for real-time communication.
/// </summary>
public partial class AIAssistantPanel : UserControl
{
    private WebView2? _webView;
    private HubConnection? _signalRConnection;
    private AgentOrchestrator? _orchestrator;
    private readonly ILogger _logger;
    private string _currentConversationId = string.Empty;

    public AIAssistantPanel()
    {
        _logger = Log.ForContext<AIAssistantPanel>();
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        this.SuspendLayout();
        
        // Create WebView2 control
        _webView = new WebView2
        {
            Dock = DockStyle.Fill,
            Location = new System.Drawing.Point(0, 0),
            Name = "webView",
            Size = this.Size,
            TabIndex = 0
        };

        this.Controls.Add(_webView);
        this.Name = "AIAssistantPanel";
        this.Size = new System.Drawing.Size(520, 800);
        
        this.ResumeLayout(false);
    }

    public async Task InitializeAsync(AgentOrchestrator orchestrator, string signalRUrl = "http://localhost:5000/agenthub")
    {
        _orchestrator = orchestrator;
        _logger.Information("Initializing AI Assistant Panel");

        try
        {
            // Initialize WebView2
            await InitializeWebView2Async();

            // Initialize SignalR
            await InitializeSignalRAsync(signalRUrl);

            _logger.Information("AI Assistant Panel initialized successfully");
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Failed to initialize AI Assistant Panel");
            MessageBox.Show($"Failed to initialize AI Assistant: {ex.Message}", "Error", 
                MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private async Task InitializeWebView2Async()
    {
        if (_webView == null) return;

        await _webView.EnsureCoreWebView2Async();
        
        // Handle web messages from JavaScript
        _webView.CoreWebView2.WebMessageReceived += OnWebMessageReceived;

        // Load embedded HTML
        var html = GetEmbeddedChatHtml();
        _webView.CoreWebView2.NavigateToString(html);

        _logger.Debug("WebView2 initialized");
    }

    private async Task InitializeSignalRAsync(string url)
    {
        _signalRConnection = new HubConnectionBuilder()
            .WithUrl(url)
            .WithAutomaticReconnect()
            .Build();

        _signalRConnection.On<string>("ReceiveToken", (token) =>
        {
            // Send token to WebView2
            _webView?.CoreWebView2.PostWebMessageAsJson(JsonSerializer.Serialize(new
            {
                type = "token",
                content = token
            }));
        });

        _signalRConnection.On<string>("ReceiveError", (error) =>
        {
            _webView?.CoreWebView2.PostWebMessageAsJson(JsonSerializer.Serialize(new
            {
                type = "error",
                content = error
            }));
        });

        await _signalRConnection.StartAsync();
        _logger.Debug("SignalR connection established");
    }

    private async void OnWebMessageReceived(object? sender, CoreWebView2WebMessageReceivedEventArgs e)
    {
        try
        {
            var json = e.WebMessageAsJson;
            var message = JsonSerializer.Deserialize<ClientMessage>(json);

            if (message == null) return;

            _logger.Debug("Received message from WebView: {Type}", message.Type);

            switch (message.Type)
            {
                case "user_query":
                    await HandleUserQueryAsync(message.Content);
                    break;

                case "clear_conversation":
                    _orchestrator?.ClearHistory();
                    _currentConversationId = Guid.NewGuid().ToString();
                    break;

                case "copy_code":
                    Clipboard.SetText(message.Content);
                    break;

                case "execute_code":
                    await HandleExecuteCodeAsync(message.Content);
                    break;

                default:
                    _logger.Warning("Unknown message type: {Type}", message.Type);
                    break;
            }
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Error handling web message");
        }
    }

    private async Task HandleUserQueryAsync(string query)
    {
        if (_orchestrator == null || _signalRConnection == null)
        {
            _logger.Error("Orchestrator or SignalR not initialized");
            return;
        }

        try
        {
            _logger.Information("Processing user query: {Query}", query);

            // Stream response token by token
            await foreach (var token in _orchestrator.StreamResponseAsync(query))
            {
                await _signalRConnection.InvokeAsync("SendToken", token);
            }

            // Signal completion
            await _signalRConnection.InvokeAsync("SendComplete");
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Error processing query");
            await _signalRConnection.InvokeAsync("SendError", ex.Message);
        }
    }

    private async Task HandleExecuteCodeAsync(string code)
    {
        try
        {
            _logger.Information("Executing code snippet");
            
            // TODO: Implement code execution logic
            // This would integrate with your Workflow+ script execution engine
            
            _webView?.CoreWebView2.PostWebMessageAsJson(JsonSerializer.Serialize(new
            {
                type = "status",
                content = "Code execution completed"
            }));
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Error executing code");
            _webView?.CoreWebView2.PostWebMessageAsJson(JsonSerializer.Serialize(new
            {
                type = "error",
                content = $"Execution failed: {ex.Message}"
            }));
        }
    }

    private string GetEmbeddedChatHtml()
    {
        try
        {
            // Try to load from embedded resources first
            var assembly = Assembly.GetExecutingAssembly();
            var resourceName = "WorkflowPlus.AIAgent.UI.WebAssets.chat.html";
            
            using var stream = assembly.GetManifestResourceStream(resourceName);
            if (stream != null)
            {
                using var reader = new StreamReader(stream);
                var html = reader.ReadToEnd();
                
                // Inject CSS and JS inline for WebView2
                html = InjectWebAssets(html);
                return html;
            }
            
            _logger.Warning("Could not load embedded HTML, trying file system");
        }
        catch (Exception ex)
        {
            _logger.Warning(ex, "Failed to load embedded resources, trying file system");
        }

        // Fallback: Try to load from file system (development mode)
        try
        {
            var basePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Agent", "UI", "WebAssets");
            var htmlPath = Path.Combine(basePath, "chat.html");
            
            if (File.Exists(htmlPath))
            {
                var html = File.ReadAllText(htmlPath);
                html = InjectWebAssetsFromFiles(html, basePath);
                return html;
            }
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Failed to load HTML from file system");
        }

        // Last resort: Return basic fallback HTML
        _logger.Error("Could not load chat UI, using fallback");
        return GetFallbackHtml();
    }

    private string InjectWebAssets(string html)
    {
        var assembly = Assembly.GetExecutingAssembly();
        
        // Load CSS
        var cssResource = "WorkflowPlus.AIAgent.UI.WebAssets.chat.css";
        using var cssStream = assembly.GetManifestResourceStream(cssResource);
        if (cssStream != null)
        {
            using var reader = new StreamReader(cssStream);
            var css = reader.ReadToEnd();
            html = html.Replace("<link rel=\"stylesheet\" href=\"chat.css\">", 
                $"<style>{css}</style>");
        }

        // Load JS
        var jsResource = "WorkflowPlus.AIAgent.UI.WebAssets.chat.js";
        using var jsStream = assembly.GetManifestResourceStream(jsResource);
        if (jsStream != null)
        {
            using var reader = new StreamReader(jsStream);
            var js = reader.ReadToEnd();
            html = html.Replace("<script src=\"chat.js\"></script>", 
                $"<script>{js}</script>");
        }

        return html;
    }

    private string InjectWebAssetsFromFiles(string html, string basePath)
    {
        // Load CSS
        var cssPath = Path.Combine(basePath, "chat.css");
        if (File.Exists(cssPath))
        {
            var css = File.ReadAllText(cssPath);
            html = html.Replace("<link rel=\"stylesheet\" href=\"chat.css\">", 
                $"<style>{css}</style>");
        }

        // Load JS
        var jsPath = Path.Combine(basePath, "chat.js");
        if (File.Exists(jsPath))
        {
            var js = File.ReadAllText(jsPath);
            html = html.Replace("<script src=\"chat.js\"></script>", 
                $"<script>{js}</script>");
        }

        return html;
    }

    private string GetFallbackHtml()
    {
        return @"<!DOCTYPE html>
<html>
<head>
    <meta charset='UTF-8'>
    <title>AI Assistant</title>
    <style>
        body { font-family: 'Segoe UI', sans-serif; padding: 20px; text-align: center; }
        .error { color: #dc2626; margin: 20px 0; }
    </style>
</head>
<body>
    <h1>AI Assistant</h1>
    <div class='error'>
        <p>⚠️ Failed to load chat interface</p>
        <p>Please check the installation and try again.</p>
    </div>
</body>
</html>";
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _signalRConnection?.DisposeAsync();
            _webView?.Dispose();
        }
        base.Dispose(disposing);
    }

    private class ClientMessage
    {
        public string Type { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
    }
}
