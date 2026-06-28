using System.Threading.RateLimiting;

namespace EvStorionX.MockStorionX.Abstractions;

/// <summary>Wraps a <see cref="RateLimiter"/> for per-request token acquisition.</summary>
public interface IRateLimiterFactory
{
    /// <summary>
    /// Attempts to acquire one token.
    /// Returns a lease; check <see cref="RateLimitLease.IsAcquired"/> before proceeding.
    /// </summary>
    ValueTask<RateLimitLease> AcquireAsync(CancellationToken ct = default);
}
