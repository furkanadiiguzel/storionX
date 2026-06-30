using Microsoft.Extensions.Logging;

namespace EvStorionX.MockEv.Generator.Generation;

/// <summary>Writes SIS part byte payloads to <c>&lt;blob-dir&gt;/&lt;part_id&gt;.bin</c>.</summary>
public sealed partial class BlobWriter(ILogger<BlobWriter> logger)
{
    public async Task WriteAsync(
        IReadOnlyList<SisPartWithData> parts,
        string blobDir,
        CancellationToken ct = default)
    {
        Directory.CreateDirectory(blobDir);

        long totalBytes = 0;
        foreach (var p in parts)
        {
            var path = Path.Combine(blobDir, $"{p.Part.PartId}.bin");
            await File.WriteAllBytesAsync(path, p.Bytes, ct);
            totalBytes += p.Bytes.Length;
        }

        LogWroteBlobs(logger, parts.Count, blobDir, totalBytes / 1_048_576.0);
    }

    [LoggerMessage(Level = LogLevel.Information,
        Message = "Wrote {Count} blobs → {Dir} ({TotalMb:F1} MB total)")]
    private static partial void LogWroteBlobs(ILogger logger, int count, string dir, double totalMb);
}
