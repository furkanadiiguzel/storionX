using EvStorionX.Domain.Enums;

namespace EvStorionX.Domain.Entities;

/// <summary>
/// Idempotency and chain-of-custody record for a single item migration attempt.
/// Persisted to the migration database; updated at each state transition.
/// </summary>
public sealed class MigrationRecord
{
    /// <summary>Stable key that uniquely identifies this migration unit across retries.</summary>
    public required string IdempotencyKey { get; init; }

    /// <summary>EV item identifier being migrated.</summary>
    public required string SourceItemId { get; init; }

    /// <summary>Source EV archive identifier.</summary>
    public required string ArchiveId { get; init; }

    /// <summary>Identifier of the storionX target archive.</summary>
    public required string TargetArchive { get; init; }

    /// <summary>storionX-assigned identifier once ingestion succeeds; <see langword="null"/> until then.</summary>
    public string? TargetId { get; init; }

    /// <summary>Current lifecycle state (see <see cref="MigrationStatus"/> for allowed transitions).</summary>
    public required MigrationStatus Status { get; init; }

    /// <summary>UTC timestamp when content was first extracted from the EV vault.</summary>
    public required DateTime ExtractedAtUtc { get; init; }

    /// <summary>UTC timestamp when the item was successfully ingested into storionX; <see langword="null"/> until then.</summary>
    public DateTime? IngestedAtUtc { get; init; }

    /// <summary>Total number of ingest attempts made for this record.</summary>
    public required int AttemptCount { get; init; }

    /// <summary>Machine-readable error code from the last failed attempt; <see langword="null"/> if no error.</summary>
    public string? LastErrorCode { get; init; }
}
