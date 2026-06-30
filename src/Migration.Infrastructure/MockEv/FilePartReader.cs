using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using EvStorionX.Application.Abstractions;
using EvStorionX.Domain.Entities;
using EvStorionX.Domain.Exceptions;
using EvStorionX.Infrastructure.Persistence;

namespace EvStorionX.Infrastructure.MockEv;

/// <summary>
/// Reads SIS part bytes from the local blob directory and part metadata from the database.
/// </summary>
public sealed partial class FilePartReader(
    MigrationDbContext db,
    IOptions<FilePartReaderOptions> options,
    ILogger<FilePartReader> logger) : IPartReader
{
    private readonly string _blobDir = options.Value.BlobDir;

    /// <inheritdoc/>
    public async Task<ReadOnlyMemory<byte>> ReadPartAsync(string partId, CancellationToken ct)
    {
        var path = Path.Combine(_blobDir, $"{partId}.bin");

        if (!File.Exists(path))
        {
            LogBlobNotFound(logger, partId, path);
            throw new PermanentMigrationException(
                "BLOB_NOT_FOUND",
                $"Blob file not found for part '{partId}' at '{path}'.");
        }

        var bytes = await File.ReadAllBytesAsync(path, ct);
        LogBlobRead(logger, bytes.Length, partId);
        return bytes.AsMemory();
    }

    /// <inheritdoc/>
    public async Task<SisPart> GetMetadataAsync(string partId, CancellationToken ct)
    {
        var part = await db.SisParts
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.PartId == partId, ct);

        if (part is null)
        {
            LogPartMetadataNotFound(logger, partId);
            throw new PermanentMigrationException(
                "PART_NOT_FOUND",
                $"SIS part metadata not found for '{partId}'.");
        }

        return part;
    }

    [LoggerMessage(Level = LogLevel.Error, Message = "Blob file missing for part {PartId}: {Path}")]
    private static partial void LogBlobNotFound(ILogger logger, string partId, string path);

    [LoggerMessage(Level = LogLevel.Debug, Message = "Read {Bytes} bytes for part {PartId}")]
    private static partial void LogBlobRead(ILogger logger, int bytes, string partId);

    [LoggerMessage(Level = LogLevel.Error, Message = "SIS part metadata not found in database: {PartId}")]
    private static partial void LogPartMetadataNotFound(ILogger logger, string partId);
}
