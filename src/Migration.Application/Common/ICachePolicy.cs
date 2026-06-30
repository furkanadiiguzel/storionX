namespace EvStorionX.Application.Common;

/// <summary>Read/write cache abstraction used by rehydration and other hot paths.</summary>
public interface ICachePolicy<TKey, TValue> where TKey : notnull
{
    /// <summary>Returns the cached value for <paramref name="key"/>, or <see langword="null"/> on a miss.</summary>
    TValue? TryGet(TKey key);

    /// <summary>Inserts or replaces the value for <paramref name="key"/>.</summary>
    void Put(TKey key, TValue value);

    /// <summary>Removes the entry for <paramref name="key"/> if present.</summary>
    void Invalidate(TKey key);

    /// <summary>Removes all cached entries.</summary>
    void Clear();
}
