namespace EvStorionX.Application.Dto;

/// <summary>Outcome of a single storionX ingest call.</summary>
public enum IngestStatus
{
    /// <summary>Item was successfully ingested.</summary>
    Ingested,

    /// <summary>Item was already present in storionX (idempotent replay).</summary>
    AlreadyPresent,

    /// <summary>Request was throttled — caller should retry after <see cref="IngestResult.RetryAfter"/>.</summary>
    RateLimited,

    /// <summary>Transient server-side error — caller may retry.</summary>
    TransientError,

    /// <summary>Permanent error — retrying will not help (4xx, schema rejection).</summary>
    PermanentError,
}

/// <summary>Structured response from <see cref="IStorionXClient.IngestAsync"/>.</summary>
public sealed record IngestResult(
    /// <summary>Outcome of the ingest attempt.</summary>
    IngestStatus Status,

    /// <summary>storionX-assigned identifier for the ingested item; <see langword="null"/> on failure.</summary>
    string? TargetId,

    /// <summary>How long the caller should wait before retrying; present on <see cref="IngestStatus.RateLimited"/> and <see cref="IngestStatus.TransientError"/>.</summary>
    TimeSpan? RetryAfter,

    /// <summary>Machine-readable error code returned by storionX; present on failure outcomes.</summary>
    string? ErrorCode
);
