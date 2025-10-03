using System.Text;

namespace WorkflowPlus.AIAgent.Tools;

/// <summary>
/// Tracks source attribution for commands and examples used in code generation
/// </summary>
public class SourceAttributionTracker
{
    private readonly Dictionary<string, SourceReference> _sources = new();
    private readonly object _lock = new();

    /// <summary>
    /// Add a source reference for a command
    /// </summary>
    public void AddSource(string commandName, string sourceFile, string? url = null, string? licenseTier = null)
    {
        lock (_lock)
        {
            if (!_sources.ContainsKey(commandName))
            {
                _sources[commandName] = new SourceReference
                {
                    CommandName = commandName,
                    SourceFile = sourceFile,
                    Url = url ?? $"https://docs.workflowplus.com/{sourceFile}",
                    LicenseTier = licenseTier ?? "Basic"
                };
            }
        }
    }

    /// <summary>
    /// Add multiple sources from command matches
    /// </summary>
    public void AddSources(IEnumerable<CommandMatch> commands)
    {
        foreach (var command in commands)
        {
            AddSource(command.Name, command.SourceFile, null, command.LicenseTier);
        }
    }

    /// <summary>
    /// Add sources from example matches
    /// </summary>
    public void AddExampleSources(IEnumerable<ExampleMatchDto> examples)
    {
        foreach (var example in examples)
        {
            var sourceName = $"Example: {example.SourceFile}";
            AddSource(sourceName, example.SourceFile, null, "Example");
        }
    }

    /// <summary>
    /// Get inline citation for a command (for use in code comments)
    /// </summary>
    public string GetInlineCitation(string commandName)
    {
        lock (_lock)
        {
            if (_sources.TryGetValue(commandName, out var source))
            {
                return $"[{commandName}]({source.Url})";
            }
            return commandName;
        }
    }

    /// <summary>
    /// Format all sources as markdown
    /// </summary>
    public string FormatSourcesMarkdown()
    {
        lock (_lock)
        {
            if (!_sources.Any())
            {
                return string.Empty;
            }

            var sb = new StringBuilder();
            sb.AppendLine("\n## Sources");
            sb.AppendLine("\nThe following documentation was used to generate this code:\n");

            // Group by license tier
            var basicSources = _sources.Values.Where(s => s.LicenseTier == "Basic").ToList();
            var premiumSources = _sources.Values.Where(s => s.LicenseTier != "Basic" && s.LicenseTier != "Example").ToList();
            var exampleSources = _sources.Values.Where(s => s.LicenseTier == "Example").ToList();

            if (basicSources.Any())
            {
                sb.AppendLine("### Commands (Basic License)");
                foreach (var source in basicSources.OrderBy(s => s.CommandName))
                {
                    sb.AppendLine($"- [{source.CommandName}]({source.Url}) - `{source.SourceFile}`");
                }
                sb.AppendLine();
            }

            if (premiumSources.Any())
            {
                sb.AppendLine($"### Commands ({premiumSources.First().LicenseTier} License Required)");
                sb.AppendLine("⚠️ **Note:** The following commands require a premium license.");
                foreach (var source in premiumSources.OrderBy(s => s.CommandName))
                {
                    sb.AppendLine($"- [{source.CommandName}]({source.Url}) - `{source.SourceFile}` ({source.LicenseTier})");
                }
                sb.AppendLine();
            }

            if (exampleSources.Any())
            {
                sb.AppendLine("### Example Scripts");
                foreach (var source in exampleSources.OrderBy(s => s.SourceFile))
                {
                    sb.AppendLine($"- `{source.SourceFile}`");
                }
                sb.AppendLine();
            }

            return sb.ToString();
        }
    }

    /// <summary>
    /// Get all sources as a dictionary
    /// </summary>
    public Dictionary<string, string> GetSourcesDictionary()
    {
        lock (_lock)
        {
            return _sources.ToDictionary(
                kvp => kvp.Key,
                kvp => kvp.Value.Url ?? kvp.Value.SourceFile
            );
        }
    }

    /// <summary>
    /// Check if any premium commands were used
    /// </summary>
    public bool HasPremiumCommands()
    {
        lock (_lock)
        {
            return _sources.Values.Any(s => s.LicenseTier != "Basic" && s.LicenseTier != "Example");
        }
    }

    /// <summary>
    /// Get list of premium commands used
    /// </summary>
    public List<string> GetPremiumCommands()
    {
        lock (_lock)
        {
            return _sources.Values
                .Where(s => s.LicenseTier != "Basic" && s.LicenseTier != "Example")
                .Select(s => $"{s.CommandName} ({s.LicenseTier})")
                .ToList();
        }
    }

    /// <summary>
    /// Clear all tracked sources
    /// </summary>
    public void Clear()
    {
        lock (_lock)
        {
            _sources.Clear();
        }
    }

    /// <summary>
    /// Get count of tracked sources
    /// </summary>
    public int Count
    {
        get
        {
            lock (_lock)
            {
                return _sources.Count;
            }
        }
    }
}

/// <summary>
/// Reference to a documentation source
/// </summary>
public class SourceReference
{
    public string CommandName { get; set; } = string.Empty;
    public string SourceFile { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
    public string LicenseTier { get; set; } = "Basic";
}
