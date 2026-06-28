using EvStorionX.MockStorionX.Abstractions;

namespace EvStorionX.MockStorionX.Handlers;

/// <summary>Handler for <c>POST /admin/reset</c>.</summary>
public static class AdminHandler
{
    /// <summary>
    /// Wipes all in-memory state (ingest records, SIS parts, counters).
    /// Intended for integration test teardown only.
    /// </summary>
    public static IResult ResetAsync(IStorionXStorage storage)
    {
        storage.Reset();
        return Results.Ok(new { reset = true, timestamp = DateTime.UtcNow });
    }
}
