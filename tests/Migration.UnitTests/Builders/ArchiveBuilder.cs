using EvStorionX.Domain.Entities;
using EvStorionX.Domain.Enums;

namespace EvStorionX.UnitTests.Builders;

internal sealed class ArchiveBuilder
{
    private string _archiveId = "archive-1";
    private ArchiveType _type = ArchiveType.Mailbox;
    private string? _ownerUpn = "alice@contoso.com";
    private bool _legalHold = false;
    private string _vaultStore = "vault-1";

    public static ArchiveBuilder Default() => new();

    public ArchiveBuilder WithId(string id) { _archiveId = id; return this; }
    public ArchiveBuilder WithType(ArchiveType t) { _type = t; return this; }
    public ArchiveBuilder WithOwnerUpn(string? upn) { _ownerUpn = upn; return this; }
    public ArchiveBuilder WithLegalHold(bool lh = true) { _legalHold = lh; return this; }
    public ArchiveBuilder WithVaultStore(string vs) { _vaultStore = vs; return this; }

    public ArchiveBuilder AsOrphan() { _ownerUpn = null; return this; }
    public ArchiveBuilder AsJournal() { _type = ArchiveType.Journal; _ownerUpn = null; return this; }
    public ArchiveBuilder AsFsa() { _type = ArchiveType.Fsa; _ownerUpn = null; return this; }

    public Archive Build() => new()
    {
        ArchiveId = _archiveId,
        Type = _type,
        OwnerUpn = _ownerUpn,
        LegalHold = _legalHold,
        VaultStore = _vaultStore,
    };
}
