namespace EvStorionX.Infrastructure.StorionXClient;

/// <summary>Configuration for the storionX HTTP client and its resilience pipeline.</summary>
public sealed class StorionXClientOptions
{
    /// <summary>Base URL of the storionX ingest API (e.g. <c>http://mockstorionx:8080</c>).</summary>
    public string BaseUrl { get; set; } = "http://localhost:8081";

    /// <summary>Maximum number of retry attempts before returning a transient failure.</summary>
    public int MaxRetryAttempts { get; set; } = 5;

    /// <summary>Base delay in milliseconds for the first retry (doubles each attempt with jitter).</summary>
    public int BaseDelayMs { get; set; } = 500;

    /// <summary>Ratio of failures required to trip the circuit breaker (0.0–1.0).</summary>
    public double CircuitBreakerFailureRatio { get; set; } = 0.5;

    /// <summary>Window in seconds over which the failure ratio is measured.</summary>
    public int CircuitBreakerSamplingSeconds { get; set; } = 30;
}
