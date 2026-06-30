using EvStorionX.Domain.Entities;

namespace EvStorionX.UnitTests.Builders;

internal sealed class ItemBuilder
{
    private string       _itemId            = "item-1";
    private string       _archiveId         = "archive-1";
    private string       _folderPath        = "Inbox";
    private string       _subject           = "Test Subject";
    private DateTime     _sentDate          = new(2023, 6, 1, 0, 0, 0, DateTimeKind.Utc);
    private string       _from              = "sender@contoso.com";
    private List<string> _to                = ["receiver@contoso.com"];
    private List<string> _cc                = [];
    private List<string> _bcc               = [];
    private List<string> _partIds           = ["part-1"];
    private string       _retentionCategory = "Standard-7Y";
    private long         _sizeBytes         = 1024;
    private string       _messageClass      = "IPM.Note";

    public static ItemBuilder Default() => new();

    public ItemBuilder WithId(string id)                   { _itemId            = id;          return this; }
    public ItemBuilder WithArchiveId(string archiveId)     { _archiveId         = archiveId;   return this; }
    public ItemBuilder WithFolderPath(string path)         { _folderPath        = path;        return this; }
    public ItemBuilder WithSubject(string s)               { _subject           = s;           return this; }
    public ItemBuilder WithSentDate(DateTime d)            { _sentDate          = d;           return this; }
    public ItemBuilder WithRetentionCategory(string cat)   { _retentionCategory = cat;         return this; }
    public ItemBuilder WithPartIds(params string[] ids)    { _partIds           = [..ids];     return this; }
    public ItemBuilder WithCc(params string[] cc)          { _cc                = [..cc];      return this; }
    public ItemBuilder WithBcc(params string[] bcc)        { _bcc               = [..bcc];     return this; }

    public Item Build() => new()
    {
        ItemId            = _itemId,
        ArchiveId         = _archiveId,
        FolderPath        = _folderPath,
        Subject           = _subject,
        SentDateUtc       = _sentDate,
        From              = _from,
        To                = _to,
        Cc                = _cc,
        Bcc               = _bcc,
        ContentPartIds    = _partIds,
        RetentionCategory = _retentionCategory,
        SizeBytes         = _sizeBytes,
        MessageClass      = _messageClass,
    };
}
