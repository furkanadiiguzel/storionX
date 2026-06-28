namespace EvStorionX.MockStorionX.Options;

/// <summary>Transient-fault injection configuration for chaos/resilience testing.</summary>
public sealed class ChaosOptions
{
    /// <summary>Percentage (0–100) of successful ingest requests that will receive a 503 instead.</summary>
    public int Transient503Percent { get; set; } = 5;

    /// <summary>
    /// Seed for the fault-injection RNG. <see langword="null"/> means non-deterministic.
    /// Set a value in tests to get reproducible fault patterns.
    /// </summary>
    public int? RandomSeed { get; set; }
}
