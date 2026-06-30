using EvStorionX.Application.Dto;
using EvStorionX.Domain.Entities;

namespace EvStorionX.Application.Abstractions;

/// <summary>Writes audit events and produces post-run reports for a migration run.</summary>
public interface IReporter
{
    /// <summary>Persists a single <see cref="AuditEvent"/> for the current run.</summary>
    Task RecordAsync(AuditEvent ev, CancellationToken ct);

    /// <summary>
    /// Aggregates all audit events for <paramref name="runId"/> into a human-readable
    /// <see cref="RunSummary"/> with counts per outcome.
    /// </summary>
    Task<RunSummary> BuildSummaryAsync(Guid runId, CancellationToken ct);

    /// <summary>
    /// Cross-references what was migrated against what storionX reports
    /// and returns any discrepancies as a <see cref="ReconciliationReport"/>.
    /// </summary>
    Task<ReconciliationReport> ReconcileAsync(Guid runId, CancellationToken ct);
}
