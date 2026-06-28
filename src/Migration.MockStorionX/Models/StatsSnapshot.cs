namespace EvStorionX.MockStorionX.Models;

/// <summary>Point-in-time statistics reported by <c>GET /stats</c>.</summary>
public sealed record StatsSnapshot(
    /// <summary>Total number of unique items successfully ingested (idempotent replays excluded).</summary>
    long TotalIngested,
    /// <summary>Number of distinct SHA-256 digests in the SIS store.</summary>
    int UniqueParts,
    /// <summary>Number of ingest requests where at least one part was a dedup hit.</summary>
    long DedupedParts,
    /// <summary>Ingest count broken down by target archive identifier.</summary>
    IReadOnlyDictionary<string, long> ByTargetArchive,
    /// <summary>Requests rejected with HTTP 429 (rate limit exceeded).</summary>
    long Rejected429Count,
    /// <summary>Requests rejected with HTTP 503 (chaos transient fault).</summary>
    long Transient503Count
);
