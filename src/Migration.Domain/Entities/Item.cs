namespace EvStorionX.Domain.Entities;

/// <summary>A single archived message or file item retrieved from an EV archive.</summary>
public sealed class Item
{
    /// <summary>EV-assigned unique identifier for this item (SavedItemId).</summary>
    public required string ItemId { get; init; }

    /// <summary>Parent archive that owns this item.</summary>
    public required string ArchiveId { get; init; }

    /// <summary>Virtual folder path within the archive (e.g. <c>Inbox\Projects</c>).</summary>
    public required string FolderPath { get; init; }

    /// <summary>Message subject line.</summary>
    public required string Subject { get; init; }

    /// <summary>Original sent date in UTC.</summary>
    public required DateTime SentDateUtc { get; init; }

    /// <summary>Sender SMTP address.</summary>
    public required string From { get; init; }

    /// <summary>Primary recipients.</summary>
    public required IReadOnlyList<string> To { get; init; }

    /// <summary>Carbon-copy recipients.</summary>
    public required IReadOnlyList<string> Cc { get; init; }

    /// <summary>Blind-carbon-copy recipients.</summary>
    public required IReadOnlyList<string> Bcc { get; init; }

    /// <summary>References to <see cref="SisPart"/> identifiers that form the content of this item.</summary>
    public required IReadOnlyList<string> ContentPartIds { get; init; }

    /// <summary>Retention category label assigned by EV policy.</summary>
    public required string RetentionCategory { get; init; }

    /// <summary>Total size of the item in bytes (before rehydration).</summary>
    public required long SizeBytes { get; init; }

    /// <summary>MAPI message class; defaults to <c>IPM.Note</c> for standard email.</summary>
    public string MessageClass { get; init; } = "IPM.Note";
}
