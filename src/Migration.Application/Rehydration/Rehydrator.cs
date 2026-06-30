using Microsoft.Extensions.Logging;
using EvStorionX.Application.Abstractions;
using EvStorionX.Application.Common;
using EvStorionX.Application.Dto;
using EvStorionX.Domain.Entities;
using EvStorionX.Domain.Exceptions;
using EvStorionX.Domain.ValueObjects;

namespace EvStorionX.Application.Rehydration;

/// <summary>
/// Fetches and assembles SIS parts into a <see cref="RehydratedItem"/>.
/// Part bytes are cached via an <see cref="ICachePolicy{TKey,TValue}"/> so that parts shared
/// across multiple items are only read from storage once per run.
/// </summary>
public sealed partial class Rehydrator(
    IPartReader partReader,
    ICachePolicy<string, byte[]> cache,
    ILogger<Rehydrator> logger) : IRehydrator
{
    /// <inheritdoc/>
    public async Task<RehydratedItem> RehydrateAsync(Item item, CancellationToken ct)
    {
        var parts = new List<RehydratedPart>(item.ContentPartIds.Count);

        foreach (var partId in item.ContentPartIds)
        {
            byte[]? cached = cache.TryGet(partId);
            ReadOnlyMemory<byte> bytes;

            if (cached is not null)
            {
                bytes = cached.AsMemory();
                LogCacheHit(logger, partId);
            }
            else
            {
                bytes = await partReader.ReadPartAsync(partId, ct);
                cache.Put(partId, bytes.ToArray());
                LogCacheMiss(logger, bytes.Length, partId);
            }

            var computed = ContentHash.FromBytes(bytes.Span);
            var metadata = await partReader.GetMetadataAsync(partId, ct);

            if (!string.Equals(computed.Hex, metadata.Sha256, StringComparison.OrdinalIgnoreCase))
            {
                LogHashMismatch(logger, partId, metadata.Sha256, computed.Hex);
                throw new PermanentMigrationException(
                    "HASH_MISMATCH",
                    $"Content hash mismatch for part '{partId}': " +
                    $"stored={metadata.Sha256}, computed={computed.Hex}.");
            }

            parts.Add(new RehydratedPart(partId, computed.Hex, metadata.SizeBytes, bytes));
        }

        return new RehydratedItem(item.ItemId, parts, DateTime.UtcNow);
    }

    [LoggerMessage(Level = LogLevel.Debug, Message = "Cache hit for part {PartId}")]
    private static partial void LogCacheHit(ILogger logger, string partId);

    [LoggerMessage(Level = LogLevel.Debug, Message = "Cache miss — fetched {Bytes} bytes for part {PartId}")]
    private static partial void LogCacheMiss(ILogger logger, int bytes, string partId);

    [LoggerMessage(Level = LogLevel.Error,
        Message = "Hash mismatch for part {PartId}: expected {Expected}, got {Actual}")]
    private static partial void LogHashMismatch(ILogger logger, string partId, string expected, string actual);
}
