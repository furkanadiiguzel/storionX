using EvStorionX.MockStorionX.Abstractions;

namespace EvStorionX.MockStorionX.Handlers;

/// <summary>Handler for <c>GET /stats</c>.</summary>
public static class StatsHandler
{
    /// <summary>Returns a point-in-time snapshot of all mock-server counters.</summary>
    public static IResult HandleAsync(IStorionXStorage storage) =>
        Results.Ok(storage.GetStats());
}
