using EvStorionX.Domain.Enums;

namespace EvStorionX.Application.Abstractions;

/// <summary>Resolves an EV mailbox owner to its target storionX archive identifier.</summary>
public interface IIdentityMap
{
    /// <summary>
    /// Returns the storionX target archive for the given UPN and archive type,
    /// or <see langword="null"/> when the mailbox is orphaned (no mapping found).
    /// </summary>
    Task<string?> ResolveTargetArchiveAsync(string ownerUpn, ArchiveType type, CancellationToken ct);
}
