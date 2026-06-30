using System.Threading.RateLimiting;
using EvStorionX.MockStorionX.Abstractions;
using EvStorionX.MockStorionX.Extensions;
using EvStorionX.MockStorionX.Models;

namespace EvStorionX.MockStorionX.Handlers;

/// <summary>Handler for <c>POST /ingest</c>.</summary>
public static class IngestHandler
{
    /// <summary>
    /// Ingests an item into the mock storionX store.
    /// Pipeline order: rate-limit → chaos → idempotency → dedup → store.
    /// </summary>
    public static async Task<IResult> HandleAsync(
        IngestRequest request,
        IRateLimiterFactory rateLimiter,
        IChaosMonkey chaos,
        IStorionXStorage storage,
        CancellationToken ct)
    {
        // ── 1. Rate limit ─────────────────────────────────────────────────────
        using var lease = await rateLimiter.AcquireAsync(ct);
        if (!lease.IsAcquired)
        {
            storage.IncrementRejected429();
            var retryAfter = lease.TryGetMetadata(MetadataName.RetryAfter, out var hint)
                ? (int)Math.Ceiling(hint.TotalSeconds)
                : 1;
            return ResultsExtensions.TooManyRequests(retryAfter);
        }

        // ── 2. Chaos fault injection (only on otherwise-successful requests) ──
        if (chaos.ShouldInjectFault())
        {
            storage.IncrementTransient503();
            return ResultsExtensions.ServiceUnavailable(retryAfterSeconds: 2);
        }

        // ── 3. Idempotency check ───────────────────────────────────────────────
        var existing = storage.FindByIdempotencyKey(request.IdempotencyKey);
        if (existing is not null)
        {
            return Results.Ok(new IngestResponse(existing.TargetId, existing.Deduped, AlreadyPresent: true));
        }

        // ── 4. Dedup content parts ────────────────────────────────────────────
        var anyDeduped = false;
        foreach (var part in request.Content.Parts)
        {
            if (storage.RecordPart(part))
                anyDeduped = true;
        }

        // ── 5. Store and respond ──────────────────────────────────────────────
        var record = storage.Add(request, anyDeduped);
        return Results.Ok(new IngestResponse(record.TargetId, record.Deduped, AlreadyPresent: false));
    }
}
