namespace EvStorionX.MockStorionX.Models;

/// <summary>In-memory record of a completed ingest operation (idempotency store).</summary>
public sealed record IngestRecord(
    string IdempotencyKey,
    string TargetId,
    string TargetArchive,
    bool Deduped,
    DateTime IngestedAtUtc
);
