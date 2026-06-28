namespace EvStorionX.Domain.Enums;

/// <summary>Physical archive kind as classified in the EV vault.</summary>
public enum ArchiveType
{
    /// <summary>Standard user mailbox archive.</summary>
    Mailbox,

    /// <summary>Journal archive capturing policy-based copies of messages.</summary>
    Journal,

    /// <summary>File System Archiving — file-based content stored in EV.</summary>
    Fsa,
}
