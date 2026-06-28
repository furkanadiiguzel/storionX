namespace EvStorionX.MockStorionX.Options;

/// <summary>Token-bucket rate-limiter configuration.</summary>
public sealed class RateLimitOptions
{
    /// <summary>Steady-state throughput: tokens replenished per second.</summary>
    public int RequestsPerSecond { get; set; } = 20;

    /// <summary>Maximum burst size (token-bucket capacity).</summary>
    public int BurstCapacity { get; set; } = 40;
}
