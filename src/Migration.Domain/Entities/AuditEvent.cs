namespace EvStorionX.Domain.Entities;

/// <summary>Immutable audit log entry capturing a single noteworthy event during a migration run.</summary>
public sealed class AuditEvent
{
    /// <summary>Surrogate primary key for this audit record.</summary>
    public required Guid Id { get; init; }

    /// <summary>UTC wall-clock time when the event occurred.</summary>
    public required DateTime TimestampUtc { get; init; }

    /// <summary>
    /// Short, machine-readable event type (e.g. <c>ItemMigrated</c>, <c>ItemFailed</c>, <c>ArchiveSkipped</c>).
    /// </summary>
    public required string EventType { get; init; }

    /// <summary>EV item identifier related to this event; <see langword="null"/> for archive- or run-level events.</summary>
    public string? ItemId { get; init; }

    /// <summary>Structured event details serialised as a JSON string.</summary>
    public required string Payload { get; init; }

    /// <summary>Identifier of the migration run that produced this event.</summary>
    public required Guid RunId { get; init; }
}
