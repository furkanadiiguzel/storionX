using System.Runtime.CompilerServices;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using EvStorionX.Application.Abstractions;
using EvStorionX.Domain.Entities;
using EvStorionX.Infrastructure.Persistence;

namespace EvStorionX.Infrastructure.MockEv;

/// <summary>EF Core-backed discovery that streams archives and items directly from MySQL.</summary>
public sealed partial class EfDiscovery(MigrationDbContext db, ILogger<EfDiscovery> logger) : IDiscovery
{
    /// <inheritdoc/>
    public async IAsyncEnumerable<Archive> ScanArchivesAsync(
        [EnumeratorCancellation] CancellationToken ct)
    {
        await foreach (var archive in db.Archives
            .AsNoTracking()
            .AsAsyncEnumerable()
            .WithCancellation(ct))
        {
            LogArchiveDiscovered(logger, archive.ArchiveId, archive.Type);
            yield return archive;
        }
    }

    /// <inheritdoc/>
    public async IAsyncEnumerable<Item> ScanItemsAsync(
        string archiveId,
        [EnumeratorCancellation] CancellationToken ct)
    {
        await foreach (var item in db.Items
            .AsNoTracking()
            .Where(i => i.ArchiveId == archiveId)
            .OrderBy(i => i.ItemId)
            .AsAsyncEnumerable()
            .WithCancellation(ct))
        {
            yield return item;
        }
    }

    [LoggerMessage(Level = LogLevel.Debug, Message = "Discovered archive {ArchiveId} (type={Type})")]
    private static partial void LogArchiveDiscovered(ILogger logger, string archiveId, EvStorionX.Domain.Enums.ArchiveType type);
}
