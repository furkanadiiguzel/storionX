namespace EvStorionX.Application.Common;

/// <summary>
/// Thread-safe Least-Recently-Used cache with a configurable capacity ceiling.
/// Concurrent reads do not block each other; writes acquire an exclusive lock.
/// </summary>
public sealed class LruCache<TKey, TValue> : ICachePolicy<TKey, TValue>, IDisposable
    where TKey : notnull
{
    private readonly int _capacity;
    private readonly ReaderWriterLockSlim _lock = new(LockRecursionPolicy.NoRecursion);
    private readonly Dictionary<TKey, LinkedListNode<(TKey Key, TValue Value)>> _map;
    private readonly LinkedList<(TKey Key, TValue Value)> _order = new();

    /// <param name="capacity">Maximum number of entries before eviction occurs. Must be ≥ 1.</param>
    public LruCache(int capacity)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(capacity, 1);
        _capacity = capacity;
        _map = new Dictionary<TKey, LinkedListNode<(TKey, TValue)>>(capacity);
    }

    /// <inheritdoc/>
    public TValue? TryGet(TKey key)
    {
        _lock.EnterUpgradeableReadLock();
        try
        {
            if (!_map.TryGetValue(key, out var node))
                return default;

            _lock.EnterWriteLock();
            try
            {
                _order.Remove(node);
                _order.AddFirst(node);
            }
            finally
            {
                _lock.ExitWriteLock();
            }

            return node.Value.Value;
        }
        finally
        {
            _lock.ExitUpgradeableReadLock();
        }
    }

    /// <inheritdoc/>
    public void Put(TKey key, TValue value)
    {
        _lock.EnterWriteLock();
        try
        {
            if (_map.TryGetValue(key, out var existing))
            {
                _order.Remove(existing);
                _map.Remove(key);
            }

            if (_map.Count >= _capacity)
                Evict();

            var node = _order.AddFirst((key, value));
            _map[key] = node;
        }
        finally
        {
            _lock.ExitWriteLock();
        }
    }

    /// <inheritdoc/>
    public void Invalidate(TKey key)
    {
        _lock.EnterWriteLock();
        try
        {
            if (_map.Remove(key, out var node))
                _order.Remove(node);
        }
        finally
        {
            _lock.ExitWriteLock();
        }
    }

    /// <inheritdoc/>
    public void Clear()
    {
        _lock.EnterWriteLock();
        try
        {
            _map.Clear();
            _order.Clear();
        }
        finally
        {
            _lock.ExitWriteLock();
        }
    }

    /// <summary>Current number of cached entries.</summary>
    public int Count
    {
        get
        {
            _lock.EnterReadLock();
            try { return _map.Count; }
            finally { _lock.ExitReadLock(); }
        }
    }

    private void Evict()
    {
        var lru = _order.Last;
        if (lru is null) return;
        _order.RemoveLast();
        _map.Remove(lru.Value.Key);
    }

    /// <inheritdoc/>
    public void Dispose() => _lock.Dispose();
}
