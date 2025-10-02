using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace WorkflowPlus.AIAgent.Observability;

/// <summary>
/// Collects metrics for monitoring agent performance.
/// </summary>
public class MetricsCollector
{
    private readonly Meter _meter;
    private readonly Counter<long> _queryCounter;
    private readonly Counter<long> _tokenCounter;
    private readonly Histogram<double> _queryDuration;
    private readonly Histogram<double> _queryCost;
    private readonly Counter<long> _errorCounter;

    public MetricsCollector(string serviceName = "WorkflowPlus.AIAgent")
    {
        _meter = new Meter(serviceName, "1.0.0");

        _queryCounter = _meter.CreateCounter<long>(
            "agent.queries.total",
            description: "Total number of queries processed");

        _tokenCounter = _meter.CreateCounter<long>(
            "agent.tokens.total",
            description: "Total number of tokens consumed");

        _queryDuration = _meter.CreateHistogram<double>(
            "agent.query.duration",
            unit: "ms",
            description: "Query processing duration in milliseconds");

        _queryCost = _meter.CreateHistogram<double>(
            "agent.query.cost",
            unit: "USD",
            description: "Query cost in USD");

        _errorCounter = _meter.CreateCounter<long>(
            "agent.errors.total",
            description: "Total number of errors");
    }

    public void RecordQuery(string userId, string model)
    {
        _queryCounter.Add(1, new KeyValuePair<string, object?>("user_id", userId),
                              new KeyValuePair<string, object?>("model", model));
    }

    public void RecordTokens(int tokens, string model)
    {
        _tokenCounter.Add(tokens, new KeyValuePair<string, object?>("model", model));
    }

    public void RecordQueryDuration(double durationMs, string model, bool success)
    {
        _queryDuration.Record(durationMs,
            new KeyValuePair<string, object?>("model", model),
            new KeyValuePair<string, object?>("success", success));
    }

    public void RecordQueryCost(decimal cost, string model)
    {
        _queryCost.Record((double)cost,
            new KeyValuePair<string, object?>("model", model));
    }

    public void RecordError(string errorType, string operation)
    {
        _errorCounter.Add(1,
            new KeyValuePair<string, object?>("error_type", errorType),
            new KeyValuePair<string, object?>("operation", operation));
    }

    public Activity? StartActivity(string operationName)
    {
        var activitySource = new ActivitySource("WorkflowPlus.AIAgent");
        return activitySource.StartActivity(operationName);
    }
}
