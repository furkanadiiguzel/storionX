using EvStorionX.Domain.Entities;

namespace EvStorionX.Application.Abstractions;

/// <summary>Streams EV vault content for discovery — archives first, then items per archive.</summary>
public interface IDiscovery
{
    /// <summary>Lazily enumerates all archives visible to the migration service account.</summary>
    IAsyncEnumerable<Archive> ScanArchivesAsync(CancellationToken ct);

    /// <summary>Lazily enumerates all items within a single <paramref name="archiveId"/>.</summary>
    IAsyncEnumerable<Item> ScanItemsAsync(string archiveId, CancellationToken ct);
}
