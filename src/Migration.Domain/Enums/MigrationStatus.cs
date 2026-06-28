namespace Migration.Domain.Enums;

/// <summary>
/// Lifecycle state of a single item as it moves through the EV→storionX pipeline.
/// <para>
/// Allowed transitions (see DESIGN.md §State Diagram):
/// <code>
/// Discovered → Mapped | Orphaned | SkippedByPolicy
/// Mapped     → Rehydrated
/// Rehydrated → Transformed
/// Transformed → Ingesting
/// Ingesting  → Migrated | Failed | AlreadyPresent
/// Failed     → Rehydrated          (retry)
/// </code>
/// </para>
/// </summary>
public enum MigrationStatus
{
    /// <summary>Item found in EV; no processing started yet.</summary>
    Discovered,

    /// <summary>Item has been mapped to a target storionX archive.</summary>
    Mapped,

    /// <summary>Source archive not resolvable to any target — requires manual review.</summary>
    Orphaned,

    /// <summary>Item excluded by a retention or migration policy rule.</summary>
    SkippedByPolicy,

    /// <summary>Raw content (shortcut/stub) has been fully rehydrated from the vault.</summary>
    Rehydrated,

    /// <summary>Content has been transformed into storionX-compatible format.</summary>
    Transformed,

    /// <summary>Ingest request submitted to storionX; awaiting confirmation.</summary>
    Ingesting,

    /// <summary>Successfully ingested into storionX.</summary>
    Migrated,

    /// <summary>A recoverable or permanent error occurred; see <c>LastErrorCode</c>.</summary>
    Failed,

    /// <summary>Item already present in storionX (detected via content hash / idempotency key).</summary>
    AlreadyPresent,
}
