using System.Collections.Concurrent;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using EvStorionX.Application.Abstractions;
using EvStorionX.Domain.Enums;

namespace EvStorionX.Infrastructure.MockEv;

/// <summary>
/// Resolves EV mailbox UPNs to storionX target archive identifiers by reading a pre-generated
/// <c>mapping.json</c> file. The file is loaded once on first access and cached for the lifetime
/// of the service.
/// </summary>
public sealed partial class JsonIdentityMap : IIdentityMap
{
    private readonly Lazy<Task<ConcurrentDictionary<string, string>>> _mapLazy;
    private readonly ILogger<JsonIdentityMap> _logger;

    public JsonIdentityMap(IOptions<JsonIdentityMapOptions> options, ILogger<JsonIdentityMap> logger)
    {
        _logger = logger;
        var path = options.Value.MappingFilePath;
        _mapLazy = new Lazy<Task<ConcurrentDictionary<string, string>>>(
            () => LoadAsync(path, logger),
            LazyThreadSafetyMode.ExecutionAndPublication);
    }

    /// <inheritdoc/>
    public async Task<string?> ResolveTargetArchiveAsync(
        string ownerUpn, ArchiveType type, CancellationToken ct)
    {
        var map = await _mapLazy.Value.WaitAsync(ct);
        var found = map.TryGetValue(ownerUpn, out var targetArchiveId);
        LogResolution(logger: _logger, upn: ownerUpn, type: type,
            result: found ? targetArchiveId! : "ORPHANED");
        return found ? targetArchiveId : null;
    }

    private static async Task<ConcurrentDictionary<string, string>> LoadAsync(
        string path, ILogger logger)
    {
        LogLoading(logger, path);

        await using var stream = File.OpenRead(path);
        using var doc = await JsonDocument.ParseAsync(stream);

        var dict = new ConcurrentDictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        foreach (var entry in doc.RootElement.GetProperty("mappings").EnumerateArray())
        {
            var upn = entry.GetProperty("upn").GetString()!;
            var targetArchive = entry.GetProperty("targetArchiveId").GetString()!;
            dict[upn] = targetArchive;
        }

        LogLoaded(logger, dict.Count, path);
        return dict;
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "Loading identity map from {Path}")]
    private static partial void LogLoading(ILogger logger, string path);

    [LoggerMessage(Level = LogLevel.Information,
        Message = "Identity map loaded: {Count} mapped UPNs from {Path}")]
    private static partial void LogLoaded(ILogger logger, int count, string path);

    [LoggerMessage(Level = LogLevel.Debug,
        Message = "Identity lookup for {Upn} ({Type}): {Result}")]
    private static partial void LogResolution(ILogger logger, string upn, ArchiveType type, string result);
}
