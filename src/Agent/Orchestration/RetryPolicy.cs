using Serilog;

namespace WorkflowPlus.AIAgent.Orchestration;

/// <summary>
/// Implements retry logic with exponential backoff for transient failures.
/// </summary>
public class RetryPolicy
{
    private readonly ILogger _logger;
    private readonly int _maxRetries;
    private readonly int _baseDelayMs;

    public RetryPolicy(ILogger logger, int maxRetries = 3, int baseDelayMs = 1000)
    {
        _logger = logger;
        _maxRetries = maxRetries;
        _baseDelayMs = baseDelayMs;
    }

    /// <summary>
    /// Execute an async operation with retry logic.
    /// </summary>
    public async Task<T> ExecuteAsync<T>(
        Func<Task<T>> operation,
        Func<Exception, bool>? isTransient = null,
        string? operationName = null)
    {
        isTransient ??= IsTransientError;
        operationName ??= "operation";

        Exception? lastException = null;

        for (int attempt = 0; attempt < _maxRetries; attempt++)
        {
            try
            {
                return await operation();
            }
            catch (Exception ex) when (isTransient(ex) && attempt < _maxRetries - 1)
            {
                lastException = ex;
                var delay = CalculateDelay(attempt);

                _logger.Warning(
                    "Transient error in {Operation} (attempt {Attempt}/{MaxRetries}): {Error}. Retrying in {Delay}ms",
                    operationName, attempt + 1, _maxRetries, ex.Message, delay);

                await Task.Delay(delay);
            }
        }

        _logger.Error(lastException, "All retry attempts failed for {Operation}", operationName);
        throw lastException ?? new InvalidOperationException("Operation failed with no exception");
    }

    private int CalculateDelay(int attempt)
    {
        // Exponential backoff: 1s, 2s, 4s, 8s, etc.
        return _baseDelayMs * (int)Math.Pow(2, attempt);
    }

    private bool IsTransientError(Exception ex)
    {
        // Check for common transient errors
        return ex is HttpRequestException ||
               ex is TaskCanceledException ||
               ex is TimeoutException ||
               (ex.Message?.Contains("rate limit", StringComparison.OrdinalIgnoreCase) ?? false) ||
               (ex.Message?.Contains("429", StringComparison.OrdinalIgnoreCase) ?? false) ||
               (ex.Message?.Contains("503", StringComparison.OrdinalIgnoreCase) ?? false);
    }
}

/// <summary>
/// Circuit breaker pattern to prevent cascading failures.
/// </summary>
public class CircuitBreaker
{
    private readonly ILogger _logger;
    private readonly int _failureThreshold;
    private readonly TimeSpan _timeout;
    private int _failureCount;
    private DateTime _lastFailureTime;
    private CircuitState _state = CircuitState.Closed;

    public CircuitBreaker(ILogger logger, int failureThreshold = 5, int timeoutSeconds = 60)
    {
        _logger = logger;
        _failureThreshold = failureThreshold;
        _timeout = TimeSpan.FromSeconds(timeoutSeconds);
    }

    public async Task<T> ExecuteAsync<T>(Func<Task<T>> operation, string serviceName)
    {
        if (_state == CircuitState.Open)
        {
            if (DateTime.UtcNow - _lastFailureTime > _timeout)
            {
                _logger.Information("Circuit breaker for {Service} entering half-open state", serviceName);
                _state = CircuitState.HalfOpen;
            }
            else
            {
                _logger.Warning("Circuit breaker for {Service} is OPEN. Request rejected.", serviceName);
                throw new InvalidOperationException($"Circuit breaker is open for {serviceName}");
            }
        }

        try
        {
            var result = await operation();

            if (_state == CircuitState.HalfOpen)
            {
                _logger.Information("Circuit breaker for {Service} closing after successful request", serviceName);
                _state = CircuitState.Closed;
                _failureCount = 0;
            }

            return result;
        }
        catch (Exception ex)
        {
            _failureCount++;
            _lastFailureTime = DateTime.UtcNow;

            _logger.Warning(
                "Circuit breaker for {Service} recorded failure {Count}/{Threshold}",
                serviceName, _failureCount, _failureThreshold);

            if (_failureCount >= _failureThreshold)
            {
                _logger.Error("Circuit breaker for {Service} is now OPEN", serviceName);
                _state = CircuitState.Open;
            }

            throw;
        }
    }

    private enum CircuitState
    {
        Closed,   // Normal operation
        Open,     // Blocking requests
        HalfOpen  // Testing if service recovered
    }
}
