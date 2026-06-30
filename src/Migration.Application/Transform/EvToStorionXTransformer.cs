using System.Text.RegularExpressions;
using Microsoft.Extensions.Options;
using EvStorionX.Application.Abstractions;
using EvStorionX.Application.Dto;
using EvStorionX.Domain.Entities;
using EvStorionX.Domain.Enums;
using EvStorionX.Domain.ValueObjects;

namespace EvStorionX.Application.Transform;

/// <summary>
/// Converts a rehydrated EV item into a <see cref="StorionXMessage"/> ready for ingest.
/// This is a pure transformation — no I/O is performed.
/// </summary>
public sealed class EvToStorionXTransformer(
    IOptions<TransformerOptions> options,
    TimeProvider timeProvider) : ITransformer
{
    private static readonly Regex RetentionYears =
        new(@"(\d+)Y$", RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private readonly TransformerOptions _opts = options.Value;

    /// <inheritdoc/>
    public StorionXMessage Transform(
        Item item, Archive archive, RehydratedItem content, string targetArchive)
    {
        var archiveClassSlug = ToArchiveClassSlug(archive.Type);
        var idempotencyKey   = IdempotencyKey.Create(archive.VaultStore, archive.ArchiveId, item.ItemId);

        var metadata = BuildMetadata(item, archive);
        var retention = new RetentionPolicy(
            item.RetentionCategory,
            ComputeExpiresUtc(item.RetentionCategory, item.SentDateUtc));

        var parts = content.Parts
            .Select(p => new MessagePart(p.PartId, p.Sha256, p.SizeBytes))
            .ToList();

        return new StorionXMessage(
            IdempotencyKey: idempotencyKey,
            TargetArchive:  targetArchive,
            ArchiveClass:   archiveClassSlug,
            Source: new MessageSource(
                System:    "EnterpriseVault",
                ArchiveId: archive.ArchiveId,
                ItemId:    item.ItemId,
                VaultStore: archive.VaultStore),
            Metadata:  metadata,
            Retention: retention,
            LegalHold: archive.LegalHold,
            Content:   new MessageContent(parts),
            ChainOfCustody: new ChainOfCustody(
                ExtractedAtUtc: timeProvider.GetUtcNow().UtcDateTime,
                RunId:          _opts.RunId,
                ToolVersion:    _opts.ToolVersion));
    }

    // ── Private helpers ───────────────────────────────────────────────────────

    private static string ToArchiveClassSlug(ArchiveType type) => type switch
    {
        ArchiveType.Mailbox => "user_mailbox",
        ArchiveType.Journal => "compliance_journal",
        ArchiveType.Fsa     => "file_archive",
        _ => throw new ArgumentOutOfRangeException(nameof(type), type, "Unknown ArchiveType.")
    };

    private static Dictionary<string, string> BuildMetadata(Item item, Archive archive)
    {
        var dict = new Dictionary<string, string>
        {
            ["subject"]       = item.Subject,
            ["from"]          = item.From,
            ["messageClass"]  = item.MessageClass,
            ["folderPath"]    = item.FolderPath,
            ["sentDateUtc"]   = item.SentDateUtc.ToString("O"),
        };

        if (item.To.Count > 0)  dict["to"]  = string.Join(";", item.To);
        if (item.Cc.Count > 0)  dict["cc"]  = string.Join(";", item.Cc);
        if (item.Bcc.Count > 0) dict["bcc"] = string.Join(";", item.Bcc);

        // Journal messages are immutable compliance copies
        if (archive.Type == ArchiveType.Journal)
            dict["immutable"] = "true";

        // FSA: carry the original folder path explicitly
        if (archive.Type == ArchiveType.Fsa)
            dict["sourceFolderPath"] = item.FolderPath;

        return dict;
    }

    private static DateTime? ComputeExpiresUtc(string category, DateTime sentDateUtc)
    {
        var match = RetentionYears.Match(category);
        if (!match.Success || !int.TryParse(match.Groups[1].Value, out var years))
            return null;
        return sentDateUtc.AddYears(years);
    }
}
