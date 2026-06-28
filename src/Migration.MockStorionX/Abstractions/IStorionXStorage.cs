using EvStorionX.MockStorionX.Models;

namespace EvStorionX.MockStorionX.Abstractions;

/// <summary>In-memory persistence for ingest records and SIS parts.</summary>
public interface IStorionXStorage
{
    /// <summary>Returns the existing record for <paramref name="key"/>, or <see langword="null"/> if unseen.</summary>
    IngestRecord? FindByIdempotencyKey(string key);

    /// <summary>
    /// Persists a new ingest record. Callers must call <see cref="FindByIdempotencyKey"/> first
    /// to guarantee idempotency.
    /// </summary>
    IngestRecord Add(IngestRequest request, bool deduped);

    /// <summary>
    /// Attempts to store a content part by its SHA-256 digest.
    /// Returns <see langword="true"/> when the digest was already known (dedup hit).
    /// </summary>
    bool RecordPart(ContentPart part);

    /// <summary>Returns a point-in-time statistics snapshot.</summary>
    StatsSnapshot GetStats();

    void IncrementRejected429();
    void IncrementTransient503();

    /// <summary>Wipes all stored state (for integration test resets).</summary>
    void Reset();
}
