namespace Migration.Domain.Entities;

/// <summary>
/// Tracks the last successfully completed phase of a migration run,
/// enabling crash-recovery and progress resumption without re-processing.
/// </summary>
public sealed class RunCheckpoint
{
    /// <summary>Identifier of the migration run this checkpoint belongs to.</summary>
    public required Guid RunId { get; init; }

    /// <summary>
    /// Name of the phase that completed (e.g. <c>ArchiveScan</c>, <c>ItemExtract</c>, <c>Ingest</c>).
    /// </summary>
    public required string Phase { get; init; }

    /// <summary>UTC wall-clock time when this checkpoint was written.</summary>
    public required DateTime CreatedAtUtc { get; init; }

    /// <summary>Optional JSON blob carrying phase-specific progress metadata.</summary>
    public string? Metadata { get; init; }
}
