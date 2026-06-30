using Microsoft.Extensions.Options;
using EvStorionX.Application.Abstractions;
using EvStorionX.Application.Dto;
using EvStorionX.Domain.Entities;
using EvStorionX.Domain.Enums;

namespace EvStorionX.Application.Mapping;

/// <summary>
/// Applies retention, legal-hold, and filter rules to decide the migration outcome for a single item.
/// All checks are synchronous and pure — no I/O is performed.
/// </summary>
public sealed class PolicyEngine(IOptions<PolicyOptions> options) : IPolicyEngine
{
    private readonly PolicyOptions _opts = options.Value;

    /// <inheritdoc/>
    public PolicyDecision Decide(Archive archive, Item item, MigrationFilters filters)
    {
        // 1. Scope filters — checked first so narrow runs are cheap
        if (filters.ArchiveId is not null && item.ArchiveId != filters.ArchiveId)
            return Skipped("ArchiveId filter not matched.");

        if (filters.FromUtc.HasValue && item.SentDateUtc < filters.FromUtc.Value)
            return Skipped("Item predates FromUtc filter.");

        if (filters.ToUtc.HasValue && item.SentDateUtc >= filters.ToUtc.Value)
            return Skipped("Item is at or after ToUtc filter.");

        if (filters.FolderPath is not null &&
            !item.FolderPath.StartsWith(filters.FolderPath, StringComparison.OrdinalIgnoreCase))
            return Skipped("FolderPath filter not matched.");

        if (filters.RetentionCategory is not null &&
            !string.Equals(item.RetentionCategory, filters.RetentionCategory, StringComparison.OrdinalIgnoreCase))
            return Skipped("RetentionCategory filter not matched.");

        // 2. Legal hold + Retain policy — blocks migration before orphan check
        if (archive.LegalHold && _opts.LegalHoldPolicy == LegalHoldPolicy.Retain)
            return new PolicyDecision(PolicyOutcome.LegalHoldRetain,
                "Archive is under legal hold; policy is Retain.");

        // 3. Orphan check — only applicable to mailbox archives (Journal/FSA have no owner)
        if (archive.Type == ArchiveType.Mailbox && archive.OwnerUpn is null)
            return new PolicyDecision(PolicyOutcome.Orphaned,
                "Mailbox archive has no owner UPN.");

        // 4. Legal hold + Migrate policy — carry hold flag into storionX
        if (archive.LegalHold && _opts.LegalHoldPolicy == LegalHoldPolicy.Migrate)
            return new PolicyDecision(PolicyOutcome.LegalHoldMigrate,
                "Archive is under legal hold; policy is Migrate.");

        // 5. Default
        return new PolicyDecision(PolicyOutcome.Proceed, "No exclusion rules matched.");
    }

    private static PolicyDecision Skipped(string reason) =>
        new(PolicyOutcome.SkippedByFilter, reason);
}
