using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace EvStorionX.MockEv.Generator.Generation;

/// <summary>
/// Writes <c>mapping.json</c> to the parent of <paramref name="blobDir"/>.
/// Orphaned UPNs are intentionally absent from the mappings array — the migration
/// pipeline will classify archives whose UPN has no mapping as <c>Orphaned</c>.
/// </summary>
public sealed partial class IdentityMapWriter(ILogger<IdentityMapWriter> logger)
{
    private static readonly JsonSerializerOptions JsonOpts = new() { WriteIndented = true };

    public async Task WriteAsync(
        GeneratedData data,
        string blobDir,
        int seed,
        CancellationToken ct = default)
    {
        var outputDir = Path.GetDirectoryName(Path.GetFullPath(blobDir)) ?? ".";
        Directory.CreateDirectory(outputDir);
        var outputPath = Path.Combine(outputDir, "mapping.json");

        var doc = new
        {
            generatedAt = DateTime.UtcNow,
            seed,
            totalMappedUpns = data.IdentityMap.Count,
            orphanedUpnCount = data.OrphanedUpns.Count,
            orphanedUpns = data.OrphanedUpns.OrderBy(u => u).ToArray(),
            mappings = data.IdentityMap
                .OrderBy(kv => kv.Key)
                .Select(kv => new { upn = kv.Key, targetArchiveId = kv.Value })
                .ToArray(),
        };

        var json = JsonSerializer.Serialize(doc, JsonOpts);
        await File.WriteAllTextAsync(outputPath, json, ct);

        LogWrote(logger, outputPath, data.IdentityMap.Count, data.OrphanedUpns.Count);
    }

    [LoggerMessage(Level = LogLevel.Information,
        Message = "Identity map → {Path}  (mapped={Mapped}, orphaned={Orphaned})")]
    private static partial void LogWrote(ILogger logger, string path, int mapped, int orphaned);
}
