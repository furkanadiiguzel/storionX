namespace EvStorionX.MockStorionX.Models;

/// <summary>Successful response from <c>POST /ingest</c>.</summary>
public sealed record IngestResponse(
    /// <summary>storionX-assigned identifier for the ingested item.</summary>
    string TargetId,
    /// <summary><c>true</c> when at least one content part was already known (SIS dedup hit).</summary>
    bool Deduped,
    /// <summary><c>true</c> when the <c>idempotencyKey</c> was already present — no duplicate was created.</summary>
    bool AlreadyPresent
);
