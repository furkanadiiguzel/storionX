using System.Collections.Concurrent;
using EvStorionX.Application.Dto;

namespace EvStorionX.Api.Services;

public enum RunStatus { Running, Completed, Failed, Cancelled }

/// <summary>Holds in-flight state for a single migration run.</summary>
public sealed class RunEntry
{
    public Guid RunId { get; }
    public Task<RunSummary> Task { get; }
    public CancellationTokenSource Cts { get; }
    public DateTimeOffset StartedAt { get; }

    private volatile int _status = (int)RunStatus.Running;
    public RunStatus Status => (RunStatus)_status;

    public RunEntry(Guid runId, Task<RunSummary> task, CancellationTokenSource cts)
    {
        RunId = runId;
        Task = task;
        Cts = cts;
        StartedAt = DateTimeOffset.UtcNow;

        _ = task.ContinueWith(t =>
        {
            var s = t.IsCanceled ? RunStatus.Cancelled
                  : t.IsFaulted ? RunStatus.Failed
                  : RunStatus.Completed;
            Interlocked.Exchange(ref _status, (int)s);
        }, TaskScheduler.Default);
    }
}

/// <summary>Thread-safe in-memory registry of active and completed migration runs.</summary>
public sealed class RunTracker
{
    private readonly ConcurrentDictionary<Guid, RunEntry> _runs = new();

    public void Add(RunEntry entry) => _runs[entry.RunId] = entry;

    public RunEntry? Get(Guid runId) => _runs.GetValueOrDefault(runId);

    public IEnumerable<RunEntry> All() => _runs.Values;

    public bool Cancel(Guid runId)
    {
        if (!_runs.TryGetValue(runId, out var entry)) return false;
        entry.Cts.Cancel();
        return true;
    }
}
