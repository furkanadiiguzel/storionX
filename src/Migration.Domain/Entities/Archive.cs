using EvStorionX.Domain.Enums;

namespace EvStorionX.Domain.Entities;

/// <summary>Represents a single EV archive vault entry — the top-level container for migrated items.</summary>
public sealed class Archive
{
    /// <summary>EV-assigned unique identifier for this archive.</summary>
    public required string ArchiveId { get; init; }

    /// <summary>Physical archive kind (Mailbox, Journal, Fsa).</summary>
    public required ArchiveType Type { get; init; }

    /// <summary>UPN of the mailbox owner; <see langword="null"/> for shared or journal archives.</summary>
    public string? OwnerUpn { get; init; }

    /// <summary>Whether this archive is under a legal hold — blocks deletion and policy-based skipping.</summary>
    public required bool LegalHold { get; init; }

    /// <summary>Identifier of the EV vault store that contains this archive.</summary>
    public required string VaultStore { get; init; }
}
