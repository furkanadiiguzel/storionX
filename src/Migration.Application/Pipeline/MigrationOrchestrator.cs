using System.Text.Json;
using System.Threading.Channels;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using EvStorionX.Application.Abstractions;
using EvStorionX.Application.Dto;
using EvStorionX.Application.Mapping;
using EvStorionX.Domain.Entities;
using EvStorionX.Domain.Enums;
using EvStorionX.Domain.Exceptions;
using EvStorionX.Domain.ValueObjects;

namespace EvStorionX.Application.Pipeline;

/// <summary>
/// Drives the complete EV→storionX migration pipeline.
/// Produces work units from the EV archive/item graph and fans them out to a bounded
/// pool of parallel workers, each running rehydrate → transform → ingest.
/// </summary>
public sealed partial class MigrationOrchestrator(
    IDiscovery discovery,
    IIdentityMap identityMap,
    IPolicyEngine policyEngine,
    IRehydrator rehydrator,
    ITransformer transformer,
    IStorionXClient storionXClient,
    IStateStore stateStore,
    IReporter reporter,
    ILogger<MigrationOrchestrator> logger,
    IOptionsSnapshot<OrchestratorOptions> options)
{
    private readonly OrchestratorOptions _opts = options.Value;

    // ── In-memory counters (Interlocked for thread-safety) ────────────────────
    private long _totalArchives;
    private long _totalItems;
    private long _migrated;
    private long _alreadyPresent;
    private long _orphaned;
    private long _skipped;
    private long _failed;
    private long _processedCount;

    private static readonly JsonSerializerOptions _jsonOpts =
        new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

    // ── Public API ────────────────────────────────────────────────────────────

    /// <summary>
    /// Executes the full migration run and returns an aggregated summary.
    /// Cancelling <paramref name="ct"/> triggers a graceful shutdown: in-flight items complete,
    /// no new items are started.
    /// </summary>
    public async Task<RunSummary> RunAsync(CancellationToken ct)
    {
        // a) Determine RunId — generate fresh if not pre-configured
        var runId = _opts.RunId == Guid.Empty ? Guid.NewGuid() : _opts.RunId;
        var startedAt = DateTime.UtcNow;

        LogRunStarted(logger, runId, _opts.MaxParallelWorkers, _opts.DryRun);
        await AuditAsync(runId, "RunStarted", null,
            Payload(new { runId, maxWorkers = _opts.MaxParallelWorkers, dryRun = _opts.DryRun }),
            ct);

        // b) Resume checkpoint (idempotency check in ConsumeAsync handles actual dedup)
        var checkpoint = await stateStore.LoadCheckpointAsync(runId, ct);
        if (checkpoint is not null)
            LogResuming(logger, runId, checkpoint.Phase);

        // c) Bounded channel — backpressure prevents unbounded memory growth
        var channelCapacity = _opts.MaxParallelWorkers * 4;
        var channel = Channel.CreateBounded<ItemWorkUnit>(new BoundedChannelOptions(channelCapacity)
        {
            FullMode = BoundedChannelFullMode.Wait,
            SingleReader = false,
            SingleWriter = true,
        });

        // d) Producer + e) consumers — both start immediately before any await
        var producerTask = ProduceAsync(runId, channel.Writer, ct);
        var consumerTask = Parallel.ForEachAsync(
            channel.Reader.ReadAllAsync(ct),
            new ParallelOptions
            {
                MaxDegreeOfParallelism = _opts.MaxParallelWorkers,
                CancellationToken = ct,
            },
            (work, workerCt) => ConsumeAsync(runId, work, workerCt));

        // f) Await both — producer closes the channel, which drains consumers naturally
        await Task.WhenAll(producerTask, consumerTask);

        // g) Final audit + summary
        await AuditAsync(runId, "RunCompleted", null,
            Payload(new
            {
                totalArchives = _totalArchives,
                totalItems = _totalItems,
                migrated = _migrated,
                alreadyPresent = _alreadyPresent,
                orphaned = _orphaned,
                skipped = _skipped,
                failed = _failed,
            }), ct);

        LogRunCompleted(logger, runId, _migrated, _failed, _skipped);

        return await reporter.BuildSummaryAsync(runId, ct);
    }

    // ── Producer ──────────────────────────────────────────────────────────────

    private async Task ProduceAsync(
        Guid runId, ChannelWriter<ItemWorkUnit> writer, CancellationToken ct)
    {
        Exception? producerError = null;
        try
        {
            await foreach (var archive in discovery.ScanArchivesAsync(ct))
            {
                Interlocked.Increment(ref _totalArchives);

                // Archive-level ArchiveId filter
                if (_opts.Filters.ArchiveId is not null
                    && archive.ArchiveId != _opts.Filters.ArchiveId)
                    continue;

                // Archive-level: legal hold + Retain policy blocks entire archive
                if (archive.LegalHold && _opts.LegalHoldPolicy == LegalHoldPolicy.Retain)
                {
                    await AuditAsync(runId, "ArchiveLegalHoldRetained", null,
                        Payload(new { archiveId = archive.ArchiveId }), ct);
                    LogArchiveSkipped(logger, archive.ArchiveId, "LegalHoldRetain");
                    continue;
                }

                // Resolve target archive (includes orphan check for Mailbox)
                var targetArchive = await ResolveTargetArchiveAsync(runId, archive, ct);
                if (targetArchive is null)
                    continue; // orphan — already audited

                // Scan and enqueue items
                await foreach (var item in discovery.ScanItemsAsync(archive.ArchiveId, ct))
                {
                    Interlocked.Increment(ref _totalItems);

                    var decision = policyEngine.Decide(archive, item, _opts.Filters);

                    if (decision.Outcome is PolicyOutcome.SkippedByFilter
                        or PolicyOutcome.Orphaned
                        or PolicyOutcome.LegalHoldRetain)
                    {
                        await AuditAsync(runId, "ItemSkipped", item.ItemId,
                            Payload(new { archiveId = archive.ArchiveId, reason = decision.Outcome.ToString() }),
                            ct);
                        Interlocked.Increment(ref _skipped);
                        continue;
                    }

                    await writer.WriteAsync(
                        new ItemWorkUnit(archive, item, targetArchive, decision), ct);
                }
            }
        }
        catch (OperationCanceledException)
        {
            // Graceful shutdown — complete channel normally so in-flight consumers finish
        }
        catch (Exception ex)
        {
            producerError = ex;
            LogProducerFailed(logger, ex);
        }
        finally
        {
            // Always complete the writer; error causes consumers to drain then fail
            writer.TryComplete(producerError);
        }
    }

    // ── Consumer ──────────────────────────────────────────────────────────────

    private async ValueTask ConsumeAsync(Guid runId, ItemWorkUnit work, CancellationToken ct)
    {
        var key = IdempotencyKey.Create(
            work.Archive.VaultStore, work.Archive.ArchiveId, work.Item.ItemId);

        // Idempotency guard — skip items already migrated in a prior run
        if (await stateStore.IsAlreadyMigratedAsync(key, ct))
        {
            LogItemAlreadyMigrated(logger, work.Item.ItemId);
            await AuditAsync(runId, "ItemAlreadyPresent", work.Item.ItemId,
                Payload(new { archiveId = work.Archive.ArchiveId, key = key.ToString() }), ct);
            Interlocked.Increment(ref _alreadyPresent);
            return;
        }

        try
        {
            // Rehydrate — fetches and verifies all SIS parts (hash-checked, cache-assisted)
            var content = await rehydrator.RehydrateAsync(work.Item, ct);

            // Transform — pure mapping, no I/O
            var msg = transformer.Transform(work.Item, work.Archive, content, work.TargetArchive);

            // Dry-run short-circuit
            if (_opts.DryRun)
            {
                LogItemDryRun(logger, work.Item.ItemId);
                await AuditAsync(runId, "ItemDryRun", work.Item.ItemId,
                    Payload(new { archiveId = work.Archive.ArchiveId }), ct);
                Interlocked.Increment(ref _migrated);
                return;
            }

            // Ingest — Polly handles 429/503 retries; exhausted retries return TransientError
            var result = await storionXClient.IngestAsync(msg, ct);

            switch (result.Status)
            {
                case IngestStatus.Ingested:
                case IngestStatus.AlreadyPresent:
                    await stateStore.MarkMigratedAsync(key, result.TargetId!, ct);
                    LogItemMigrated(logger, work.Item.ItemId, result.TargetId!);
                    await AuditAsync(runId, "ItemMigrated", work.Item.ItemId,
                        Payload(new
                        {
                            archiveId = work.Archive.ArchiveId,
                            targetId = result.TargetId,
                            status = result.Status.ToString(),
                        }), ct);
                    Interlocked.Increment(ref _migrated);
                    break;

                case IngestStatus.TransientError:
                    LogItemTransientFailed(logger, work.Item.ItemId, result.ErrorCode);
                    await AuditAsync(runId, "ItemTransientFailed", work.Item.ItemId,
                        Payload(new { archiveId = work.Archive.ArchiveId, errorCode = result.ErrorCode }),
                        ct);
                    Interlocked.Increment(ref _failed);
                    break;

                case IngestStatus.PermanentError:
                    LogItemPermanentFailed(logger, work.Item.ItemId, result.ErrorCode);
                    await AuditAsync(runId, "ItemPermanentFailed", work.Item.ItemId,
                        Payload(new { archiveId = work.Archive.ArchiveId, errorCode = result.ErrorCode }),
                        ct);
                    Interlocked.Increment(ref _failed);
                    break;
            }
        }
        catch (PermanentMigrationException ex)
        {
            // Non-retriable domain error (e.g. BLOB_NOT_FOUND, HASH_MISMATCH)
            LogItemDomainError(logger, work.Item.ItemId, ex.ErrorCode, ex);
            await AuditAsync(runId, "ItemPermanentFailed", work.Item.ItemId,
                Payload(new { archiveId = work.Archive.ArchiveId, errorCode = ex.ErrorCode, message = ex.Message }),
                ct);
            Interlocked.Increment(ref _failed);
        }
        catch (Exception ex)
        {
            // Unexpected error — log, audit, and continue so one bad item never aborts the run
            LogItemUnexpectedError(logger, work.Item.ItemId, ex);
            await AuditAsync(runId, "ItemUnexpectedError", work.Item.ItemId,
                Payload(new { archiveId = work.Archive.ArchiveId, error = ex.GetType().Name, message = ex.Message }),
                ct);
            Interlocked.Increment(ref _failed);
        }

        // Checkpoint every N processed items
        var processed = Interlocked.Increment(ref _processedCount);
        if (processed % _opts.CheckpointEveryN == 0)
        {
            await stateStore.SaveCheckpointAsync(new RunCheckpoint
            {
                RunId = runId,
                Phase = "Ingest",
                CreatedAtUtc = DateTime.UtcNow,
                Metadata = Payload(new { processedCount = processed }),
            }, ct);
        }
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private async Task<string?> ResolveTargetArchiveAsync(
        Guid runId, Archive archive, CancellationToken ct)
    {
        if (archive.Type == ArchiveType.Mailbox)
        {
            if (archive.OwnerUpn is null)
            {
                await AuditAsync(runId, "ArchiveOrphaned", null,
                    Payload(new { archiveId = archive.ArchiveId, reason = "NullOwnerUpn" }), ct);
                LogArchiveSkipped(logger, archive.ArchiveId, "OrphanedNullUpn");
                Interlocked.Increment(ref _orphaned);
                return null;
            }

            var target = await identityMap.ResolveTargetArchiveAsync(archive.OwnerUpn, archive.Type, ct);
            if (target is null)
            {
                await AuditAsync(runId, "ArchiveOrphaned", null,
                    Payload(new { archiveId = archive.ArchiveId, ownerUpn = archive.OwnerUpn }), ct);
                LogArchiveSkipped(logger, archive.ArchiveId, "Orphaned");
                Interlocked.Increment(ref _orphaned);
                return null;
            }

            return target;
        }

        // Journal / FSA — no UPN; use type-based prefix + archiveId
        return archive.Type switch
        {
            ArchiveType.Journal => $"compliance_journal:{archive.ArchiveId}",
            ArchiveType.Fsa => $"file_archive:{archive.ArchiveId}",
            _ => $"unknown:{archive.ArchiveId}",
        };
    }

    private Task AuditAsync(
        Guid runId, string eventType, string? itemId, string payload, CancellationToken ct) =>
        reporter.RecordAsync(new AuditEvent
        {
            Id = Guid.NewGuid(),
            TimestampUtc = DateTime.UtcNow,
            EventType = eventType,
            ItemId = itemId,
            Payload = payload,
            RunId = runId,
        }, ct);

    private static string Payload<T>(T obj) =>
        JsonSerializer.Serialize(obj, _jsonOpts);

    // ── LoggerMessage delegates ───────────────────────────────────────────────

    [LoggerMessage(Level = LogLevel.Information,
        Message = "Migration run {RunId} started — workers={Workers}, dryRun={DryRun}")]
    private static partial void LogRunStarted(ILogger l, Guid runId, int workers, bool dryRun);

    [LoggerMessage(Level = LogLevel.Information,
        Message = "Resuming run {RunId} from checkpoint phase '{Phase}'")]
    private static partial void LogResuming(ILogger l, Guid runId, string phase);

    [LoggerMessage(Level = LogLevel.Warning,
        Message = "Archive {ArchiveId} skipped: {Reason}")]
    private static partial void LogArchiveSkipped(ILogger l, string archiveId, string reason);

    [LoggerMessage(Level = LogLevel.Error,
        Message = "Producer failed; channel closed with error")]
    private static partial void LogProducerFailed(ILogger l, Exception ex);

    [LoggerMessage(Level = LogLevel.Debug,
        Message = "Item {ItemId} already migrated in a prior run — skipping")]
    private static partial void LogItemAlreadyMigrated(ILogger l, string itemId);

    [LoggerMessage(Level = LogLevel.Debug,
        Message = "Item {ItemId} dry-run only — no ingest submitted")]
    private static partial void LogItemDryRun(ILogger l, string itemId);

    [LoggerMessage(Level = LogLevel.Information,
        Message = "Item {ItemId} migrated → {TargetId}")]
    private static partial void LogItemMigrated(ILogger l, string itemId, string targetId);

    [LoggerMessage(Level = LogLevel.Warning,
        Message = "Item {ItemId} transient ingest failure: {ErrorCode}")]
    private static partial void LogItemTransientFailed(ILogger l, string itemId, string? errorCode);

    [LoggerMessage(Level = LogLevel.Error,
        Message = "Item {ItemId} permanent ingest failure: {ErrorCode}")]
    private static partial void LogItemPermanentFailed(ILogger l, string itemId, string? errorCode);

    [LoggerMessage(Level = LogLevel.Error,
        Message = "Item {ItemId} domain error: {ErrorCode}")]
    private static partial void LogItemDomainError(ILogger l, string itemId, string errorCode, Exception ex);

    [LoggerMessage(Level = LogLevel.Error,
        Message = "Item {ItemId} unexpected error — item skipped, run continues")]
    private static partial void LogItemUnexpectedError(ILogger l, string itemId, Exception ex);

    [LoggerMessage(Level = LogLevel.Information,
        Message = "Run {RunId} completed — migrated={Migrated}, failed={Failed}, skipped={Skipped}")]
    private static partial void LogRunCompleted(ILogger l, Guid runId, long migrated, long failed, long skipped);
}
