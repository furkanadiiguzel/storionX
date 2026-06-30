using System.Collections.Concurrent;
using System.Runtime.CompilerServices;
using System.Text.Json;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using EvStorionX.Application.Abstractions;
using EvStorionX.Application.Dto;
using EvStorionX.Application.Mapping;
using EvStorionX.Application.Pipeline;
using EvStorionX.Application.Transform;
using EvStorionX.Domain.Entities;
using EvStorionX.Domain.Enums;
using EvStorionX.Domain.ValueObjects;
using EvStorionX.UnitTests.Builders;

namespace EvStorionX.UnitTests;

public sealed class Orchestrator_Tests
{
    // ── Fakes ─────────────────────────────────────────────────────────────────

    private sealed class FakeOptionsSnapshot<T>(T value) : IOptionsSnapshot<T>
        where T : class, new()
    {
        public T Value => value;
        public T Get(string? name) => value;
    }

    private sealed class InMemoryStateStore : IStateStore
    {
        private readonly ConcurrentDictionary<string, string> _migrated = new();

        public Task<bool> IsAlreadyMigratedAsync(IdempotencyKey key, CancellationToken ct) =>
            Task.FromResult(_migrated.ContainsKey(key.ToString()));

        public Task MarkMigratedAsync(IdempotencyKey key, string targetId, CancellationToken ct)
        {
            _migrated[key.ToString()] = targetId;
            return Task.CompletedTask;
        }

        public Task<RunCheckpoint?> LoadCheckpointAsync(Guid runId, CancellationToken ct) =>
            Task.FromResult<RunCheckpoint?>(null);

        public Task SaveCheckpointAsync(RunCheckpoint checkpoint, CancellationToken ct) =>
            Task.CompletedTask;
    }

    private sealed class InMemoryReporter : IReporter
    {
        private readonly List<AuditEvent> _events = [];
        private readonly object _lock = new();

        public IReadOnlyList<AuditEvent> Events { get { lock (_lock) return [.. _events]; } }

        public Task RecordAsync(AuditEvent ev, CancellationToken ct)
        {
            lock (_lock) _events.Add(ev);
            return Task.CompletedTask;
        }

        public Task<RunSummary> BuildSummaryAsync(Guid runId, CancellationToken ct)
        {
            List<AuditEvent> evs;
            lock (_lock) evs = [.. _events.Where(e => e.RunId == runId)];

            var startedAt = evs.FirstOrDefault(e => e.EventType == "RunStarted")?.TimestampUtc ?? DateTime.UtcNow;
            var finishedAt = evs.FirstOrDefault(e => e.EventType == "RunCompleted")?.TimestampUtc;
            var payload = evs.FirstOrDefault(e => e.EventType == "RunCompleted")?.Payload;

            long totalArchives = 0, totalItems = 0, migrated = 0,
                 alreadyPresent = 0, orphaned = 0, skipped = 0, failed = 0;

            if (payload is not null)
            {
                using var doc = JsonDocument.Parse(payload);
                var root = doc.RootElement;
                totalArchives = GetLong(root, "totalArchives");
                totalItems = GetLong(root, "totalItems");
                migrated = GetLong(root, "migrated");
                alreadyPresent = GetLong(root, "alreadyPresent");
                orphaned = GetLong(root, "orphaned");
                skipped = GetLong(root, "skipped");
                failed = GetLong(root, "failed");
            }

            return Task.FromResult(new RunSummary(runId, startedAt, finishedAt,
                (int)totalArchives, totalItems,
                migrated, alreadyPresent, orphaned, skipped, failed,
                new Dictionary<string, ArchiveSummary>()));
        }

        public Task<ReconciliationReport> ReconcileAsync(Guid runId, CancellationToken ct) =>
            throw new NotSupportedException();

        private static long GetLong(JsonElement root, string name) =>
            root.TryGetProperty(name, out var el) ? el.GetInt64() : 0L;
    }

    private sealed class FakeDiscovery(
        IReadOnlyList<Archive> archives,
        IReadOnlyDictionary<string, IReadOnlyList<Item>> itemsByArchive) : IDiscovery
    {
        public async IAsyncEnumerable<Archive> ScanArchivesAsync(
            [EnumeratorCancellation] CancellationToken ct)
        {
            foreach (var a in archives)
            {
                ct.ThrowIfCancellationRequested();
                yield return a;
                await Task.Yield();
            }
        }

        public async IAsyncEnumerable<Item> ScanItemsAsync(
            string archiveId,
            [EnumeratorCancellation] CancellationToken ct)
        {
            if (!itemsByArchive.TryGetValue(archiveId, out var items))
                yield break;
            foreach (var item in items)
            {
                ct.ThrowIfCancellationRequested();
                yield return item;
                await Task.Yield();
            }
        }
    }

    private sealed class FakeIdentityMap(string? resolvedTarget = "user_mailbox:alice@contoso.com")
        : IIdentityMap
    {
        public Task<string?> ResolveTargetArchiveAsync(string ownerUpn, ArchiveType type, CancellationToken ct) =>
            Task.FromResult(resolvedTarget);
    }

    private sealed class FakeRehydrator : IRehydrator
    {
        public Task<RehydratedItem> RehydrateAsync(Item item, CancellationToken ct) =>
            Task.FromResult(new RehydratedItem(
                item.ItemId,
                [new RehydratedPart("p1", new string('a', 64), 1, new byte[] { 0x01 }.AsMemory())],
                DateTime.UtcNow));
    }

    private sealed class FakeStorionXClient(
        Func<StorionXMessage, IngestResult>? resultFactory = null) : IStorionXClient
    {
        public Task<IngestResult> IngestAsync(StorionXMessage message, CancellationToken ct) =>
            Task.FromResult(resultFactory?.Invoke(message)
                ?? new IngestResult(IngestStatus.Ingested, $"tgt-{Guid.NewGuid():N}", null, null));

        public Task<StorionXStats?> GetStatsAsync(CancellationToken ct) =>
            Task.FromResult<StorionXStats?>(null);
    }

    // ── Factory ───────────────────────────────────────────────────────────────

    private static MigrationOrchestrator MakeSut(
        FakeDiscovery discovery,
        IStorionXClient? client = null,
        IStateStore? stateStore = null,
        InMemoryReporter? reporter = null,
        OrchestratorOptions? opts = null)
    {
        var effectiveOpts = opts ?? new OrchestratorOptions
        {
            MaxParallelWorkers = 2,
            CheckpointEveryN = 10_000,
        };

        var transformer = new EvToStorionXTransformer(
            Options.Create(new TransformerOptions { ToolVersion = "1.0.0", RunId = effectiveOpts.RunId }),
            TimeProvider.System);

        return new MigrationOrchestrator(
            discovery,
            new FakeIdentityMap(),
            new PolicyEngine(Options.Create(new PolicyOptions())),
            new FakeRehydrator(),
            transformer,
            client ?? new FakeStorionXClient(),
            stateStore ?? new InMemoryStateStore(),
            reporter ?? new InMemoryReporter(),
            NullLogger<MigrationOrchestrator>.Instance,
            new FakeOptionsSnapshot<OrchestratorOptions>(effectiveOpts));
    }

    // ── Tests ─────────────────────────────────────────────────────────────────

    [Fact]
    public async Task RunAsync_TwoNormalArchivesAndOneOrphan_OrphanSkippedNormalItemsMigrated()
    {
        // 2 normal mailbox archives × 4 items + 1 orphaned archive (items never scanned)
        var a1 = ArchiveBuilder.Default().WithId("a1").WithOwnerUpn("alice@c.com").Build();
        var a2 = ArchiveBuilder.Default().WithId("a2").WithOwnerUpn("bob@c.com").Build();
        var orphan = ArchiveBuilder.Default().WithId("orphan").AsOrphan().Build();

        var items1 = Enumerable.Range(1, 4)
            .Select(i => ItemBuilder.Default().WithId($"item-a1-{i}").WithArchiveId("a1").Build())
            .ToList();
        var items2 = Enumerable.Range(1, 4)
            .Select(i => ItemBuilder.Default().WithId($"item-a2-{i}").WithArchiveId("a2").Build())
            .ToList();
        var orphanItems = Enumerable.Range(1, 2)
            .Select(i => ItemBuilder.Default().WithId($"orphan-{i}").WithArchiveId("orphan").Build())
            .ToList();

        var discovery = new FakeDiscovery(
            [a1, a2, orphan],
            new Dictionary<string, IReadOnlyList<Item>>
            {
                ["a1"] = items1,
                ["a2"] = items2,
                ["orphan"] = orphanItems,
            });

        var summary = await MakeSut(discovery).RunAsync(CancellationToken.None);

        summary.Migrated.Should().Be(8, "4 items per normal archive × 2 archives");
        summary.Orphaned.Should().Be(1, "one orphaned archive");
        summary.Failed.Should().Be(0);
    }

    [Fact]
    public async Task RunAsync_OnePermanentErrorItem_OtherItemsStillMigrate()
    {
        var archive = ArchiveBuilder.Default().WithId("a1").WithOwnerUpn("alice@c.com").Build();
        var items = Enumerable.Range(1, 5)
            .Select(i => ItemBuilder.Default().WithId($"item-{i}").WithArchiveId("a1").Build())
            .ToList();

        var discovery = new FakeDiscovery(
            [archive],
            new Dictionary<string, IReadOnlyList<Item>> { ["a1"] = items });

        var callIndex = 0;
        var client = new FakeStorionXClient(msg =>
        {
            var n = Interlocked.Increment(ref callIndex);
            return n == 3
                ? new IngestResult(IngestStatus.PermanentError, null, null, "REJECTED")
                : new IngestResult(IngestStatus.Ingested, $"tgt-{n}", null, null);
        });

        var summary = await MakeSut(discovery, client: client).RunAsync(CancellationToken.None);

        summary.Migrated.Should().Be(4, "4 of 5 items ingested successfully");
        summary.Failed.Should().Be(1, "one item returned PermanentError");
    }

    [Fact]
    public async Task RunAsync_SecondRunSameItems_AllServedFromStateStore()
    {
        var archive = ArchiveBuilder.Default().WithId("a1").WithOwnerUpn("alice@c.com").Build();
        var items = Enumerable.Range(1, 5)
            .Select(i => ItemBuilder.Default().WithId($"item-{i}").WithArchiveId("a1").Build())
            .ToList();

        var discovery = new FakeDiscovery(
            [archive],
            new Dictionary<string, IReadOnlyList<Item>> { ["a1"] = items });

        var stateStore = new InMemoryStateStore();
        var reporter = new InMemoryReporter();

        // First run — populates the state store
        var summary1 = await MakeSut(discovery, stateStore: stateStore, reporter: reporter)
            .RunAsync(CancellationToken.None);
        summary1.Migrated.Should().Be(5);

        // Second run — every item is already in the state store
        var summary2 = await MakeSut(discovery, stateStore: stateStore, reporter: reporter)
            .RunAsync(CancellationToken.None);

        summary2.Migrated.Should().Be(0, "no new items this run");
        summary2.AlreadyPresent.Should().Be(5, "all items were found in the state store");
    }
}
