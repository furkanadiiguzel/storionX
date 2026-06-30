namespace EvStorionX.Application.Dto;

/// <summary>Aggregated outcome statistics for a completed migration run.</summary>
public sealed record RunSummary(
    /// <summary>Identifier of the migration run.</summary>
    Guid RunId,

    /// <summary>UTC time the run started.</summary>
    DateTime StartedAtUtc,

    /// <summary>UTC time the run finished; <see langword="null"/> if still in progress.</summary>
    DateTime? FinishedAtUtc,

    /// <summary>Total archives scanned.</summary>
    int TotalArchives,

    /// <summary>Total items discovered across all archives.</summary>
    long TotalItems,

    /// <summary>Items successfully ingested into storionX.</summary>
    long Migrated,

    /// <summary>Items that were already present in storionX (idempotent replays).</summary>
    long AlreadyPresent,

    /// <summary>Items skipped because they belong to orphaned mailboxes.</summary>
    long Orphaned,

    /// <summary>Items skipped by policy or filter.</summary>
    long Skipped,

    /// <summary>Items that failed after all retries.</summary>
    long Failed,

    /// <summary>Per-archive breakdown of outcomes.</summary>
    IReadOnlyDictionary<string, ArchiveSummary> ByArchive
);

/// <summary>Per-archive outcome breakdown within a run summary.</summary>
public sealed record ArchiveSummary(
    long Migrated,
    long AlreadyPresent,
    long Skipped,
    long Failed
);
