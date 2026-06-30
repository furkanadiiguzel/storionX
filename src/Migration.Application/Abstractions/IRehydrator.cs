using EvStorionX.Application.Dto;
using EvStorionX.Domain.Entities;

namespace EvStorionX.Application.Abstractions;

/// <summary>
/// Expands a stub/shortcut <see cref="Item"/> into its full content by fetching and assembling SIS parts.
/// Implementations cache results via an injected <see cref="Common.ICachePolicy{TKey,TValue}"/>.
/// </summary>
public interface IRehydrator
{
    /// <summary>
    /// Returns the fully rehydrated content for <paramref name="item"/>.
    /// Repeated calls for the same item return the cached result without re-fetching.
    /// </summary>
    Task<RehydratedItem> RehydrateAsync(Item item, CancellationToken ct);
}
