namespace EvStorionX.MockStorionX.Abstractions;

/// <summary>Decides whether a given request should receive a transient 503 fault.</summary>
public interface IChaosMonkey
{
    /// <summary>
    /// Returns <see langword="true"/> when a fault should be injected.
    /// Only called after the request would otherwise succeed.
    /// </summary>
    bool ShouldInjectFault();
}
