using EvStorionX.MockStorionX.Abstractions;
using EvStorionX.MockStorionX.Options;
using Microsoft.Extensions.Options;

namespace EvStorionX.MockStorionX.Chaos;

/// <summary>
/// Injects transient 503 faults with probability <c>Transient503Percent / 100</c>.
/// Uses a seeded <see cref="Random"/> when <see cref="ChaosOptions.RandomSeed"/> is set,
/// otherwise defers to <see cref="Random.Shared"/> (non-deterministic, thread-safe).
/// </summary>
public sealed class RandomChaosMonkey : IChaosMonkey
{
    private readonly ChaosOptions _opts;
    private readonly Random? _seededRng; // non-null only when a seed was configured
    private readonly object _lock = new();

    public RandomChaosMonkey(IOptions<ChaosOptions> options)
    {
        _opts = options.Value;
        if (_opts.RandomSeed.HasValue)
            _seededRng = new Random(_opts.RandomSeed.Value);
    }

    public bool ShouldInjectFault()
    {
        if (_opts.Transient503Percent <= 0) return false;

        int roll;
        if (_seededRng is not null)
        {
            lock (_lock) roll = _seededRng.Next(100);
        }
        else
        {
            roll = Random.Shared.Next(100);
        }

        return roll < _opts.Transient503Percent;
    }
}
