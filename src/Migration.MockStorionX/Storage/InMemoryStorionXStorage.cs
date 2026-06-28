using System.Collections.Concurrent;
using EvStorionX.MockStorionX.Abstractions;
using EvStorionX.MockStorionX.Models;

namespace EvStorionX.MockStorionX.Storage;

/// <summary>
/// Thread-safe, in-memory implementation of <see cref="IStorionXStorage"/>.
/// All state is lost on restart — this is intentional for a mock server.
/// </summary>
public sealed class InMemoryStorionXStorage : IStorionXStorage
{
    private ConcurrentDictionary<string, IngestRecord> _records = new();
    private ConcurrentDictionary<string, StoredPart>   _parts   = new();
    private ConcurrentDictionary<string, long>         _byArchive = new();
    private long _dedupedCount;
    private long _rejected429;
    private long _transient503;

    public IngestRecord? FindByIdempotencyKey(string key) =>
        _records.TryGetValue(key, out var r) ? r : null;

    public IngestRecord Add(IngestRequest request, bool deduped)
    {
        var record = new IngestRecord(
            request.IdempotencyKey,
            GenerateTargetId(),
            request.TargetArchive,
            deduped,
            DateTime.UtcNow);

        _records[request.IdempotencyKey] = record;
        _byArchive.AddOrUpdate(request.TargetArchive, 1L, (_, v) => v + 1);
        if (deduped) Interlocked.Increment(ref _dedupedCount);

        return record;
    }

    public bool RecordPart(ContentPart part)
    {
        var stored = new StoredPart(part.PartId, part.Sha256, part.SizeBytes, DateTime.UtcNow);
        bool isNew = _parts.TryAdd(part.Sha256, stored);
        return !isNew; // true = dedup hit
    }

    public StatsSnapshot GetStats() => new(
        TotalIngested:     _records.Count,
        UniqueParts:       _parts.Count,
        DedupedParts:      Interlocked.Read(ref _dedupedCount),
        ByTargetArchive:   new Dictionary<string, long>(_byArchive),
        Rejected429Count:  Interlocked.Read(ref _rejected429),
        Transient503Count: Interlocked.Read(ref _transient503));

    public void IncrementRejected429()   => Interlocked.Increment(ref _rejected429);
    public void IncrementTransient503()  => Interlocked.Increment(ref _transient503);

    public void Reset()
    {
        _records   = new ConcurrentDictionary<string, IngestRecord>();
        _parts     = new ConcurrentDictionary<string, StoredPart>();
        _byArchive = new ConcurrentDictionary<string, long>();
        Interlocked.Exchange(ref _dedupedCount, 0);
        Interlocked.Exchange(ref _rejected429,  0);
        Interlocked.Exchange(ref _transient503, 0);
    }

    private static string GenerateTargetId() =>
        $"sx-{Guid.NewGuid():N}"[..11]; // "sx-" + 8 hex chars → "sx-xxxxxxxx"
}
