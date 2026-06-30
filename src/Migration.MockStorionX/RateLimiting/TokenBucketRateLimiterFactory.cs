using System.Threading.RateLimiting;
using EvStorionX.MockStorionX.Abstractions;
using EvStorionX.MockStorionX.Options;
using Microsoft.Extensions.Options;

namespace EvStorionX.MockStorionX.RateLimiting;

/// <summary>
/// Wraps a <see cref="TokenBucketRateLimiter"/> configured from <see cref="RateLimitOptions"/>.
/// Burst capacity = bucket size; steady-state = tokens replenished per second.
/// </summary>
public sealed class TokenBucketRateLimiterFactory : IRateLimiterFactory, IDisposable
{
    private readonly TokenBucketRateLimiter _limiter;

    public TokenBucketRateLimiterFactory(IOptions<RateLimitOptions> options)
    {
        var o = options.Value;
        _limiter = new TokenBucketRateLimiter(new TokenBucketRateLimiterOptions
        {
            TokenLimit = o.BurstCapacity,
            TokensPerPeriod = o.RequestsPerSecond,
            ReplenishmentPeriod = TimeSpan.FromSeconds(1),
            QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
            QueueLimit = 0,           // reject immediately; never queue
            AutoReplenishment = true,
        });
    }

    public ValueTask<RateLimitLease> AcquireAsync(CancellationToken ct = default) =>
        _limiter.AcquireAsync(permitCount: 1, ct);

    public void Dispose() => _limiter.Dispose();
}
