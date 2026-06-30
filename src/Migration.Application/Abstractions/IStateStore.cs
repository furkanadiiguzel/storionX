using EvStorionX.Application.Dto;
using EvStorionX.Domain.Entities;
using EvStorionX.Domain.ValueObjects;

namespace EvStorionX.Application.Abstractions;

/// <summary>Persists migration progress state to support idempotency and crash recovery.</summary>
public interface IStateStore
{
    /// <summary>Returns <see langword="true"/> when <paramref name="key"/> has already been successfully migrated.</summary>
    Task<bool> IsAlreadyMigratedAsync(IdempotencyKey key, CancellationToken ct);

    /// <summary>Records <paramref name="key"/> as migrated with the given storionX <paramref name="targetId"/>.</summary>
    Task MarkMigratedAsync(IdempotencyKey key, string targetId, CancellationToken ct);

    /// <summary>Loads the latest checkpoint for <paramref name="runId"/>, or <see langword="null"/> if none exists.</summary>
    Task<RunCheckpoint?> LoadCheckpointAsync(Guid runId, CancellationToken ct);

    /// <summary>Persists <paramref name="checkpoint"/>, overwriting any prior checkpoint for the same run and phase.</summary>
    Task SaveCheckpointAsync(RunCheckpoint checkpoint, CancellationToken ct);
}
