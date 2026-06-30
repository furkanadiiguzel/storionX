using EvStorionX.Application.Dto;
using EvStorionX.Application.Mapping;

namespace EvStorionX.Application.Pipeline;

/// <summary>Configuration for <see cref="MigrationOrchestrator"/>.</summary>
public sealed class OrchestratorOptions
{
    /// <summary>Maximum number of items processed concurrently by the worker pool.</summary>
    public int MaxParallelWorkers { get; set; } = 8;

    /// <summary>How archives under legal hold are handled.</summary>
    public LegalHoldPolicy LegalHoldPolicy { get; set; } = LegalHoldPolicy.Retain;

    /// <summary>
    /// When <see langword="true"/> the pipeline runs end-to-end but no ingest calls are made.
    /// Useful for estimating scope without modifying storionX.
    /// </summary>
    public bool DryRun { get; set; }

    /// <summary>
    /// Identifier for the current migration run.
    /// Defaults to <see cref="Guid.Empty"/>; the orchestrator generates a fresh ID when empty.
    /// </summary>
    public Guid RunId { get; set; } = Guid.Empty;

    /// <summary>Scope filters applied to archives and items during the run.</summary>
    public MigrationFilters Filters { get; set; } = new();

    /// <summary>Writes a progress checkpoint to <see cref="IStateStore"/> after every N processed items.</summary>
    public int CheckpointEveryN { get; set; } = 100;
}
