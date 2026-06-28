namespace Migration.Domain.Enums;

/// <summary>Logical classification of an archive, independent of its physical type.</summary>
public enum ArchiveClass
{
    /// <summary>Belongs to a single named user mailbox.</summary>
    UserMailbox,

    /// <summary>Compliance journal — regulatory retention scope.</summary>
    ComplianceJournal,

    /// <summary>File-system archiving content.</summary>
    FileArchive,

    /// <summary>Team / group collaboration content (e.g. public folders, Teams).</summary>
    Collaboration,

    /// <summary>Shared mailbox not owned by a single user.</summary>
    Shared,
}
