namespace EvStorionX.Application.Dto;

/// <summary>Full ingest payload submitted to the storionX API.</summary>
public sealed record StorionXMessage(
    /// <summary>Stable, content-addressed key used by storionX for idempotent ingest.</summary>
    string IdempotencyKey,

    /// <summary>Target archive identifier in storionX (e.g. <c>user_mailbox:alice@contoso.com</c>).</summary>
    string TargetArchive,

    /// <summary>Logical classification of the archive (e.g. <c>user_mailbox</c>).</summary>
    string ArchiveClass,

    /// <summary>Source system provenance metadata.</summary>
    MessageSource Source,

    /// <summary>Arbitrary key/value metadata forwarded to storionX.</summary>
    IReadOnlyDictionary<string, string> Metadata,

    /// <summary>Retention policy properties.</summary>
    RetentionPolicy Retention,

    /// <summary>Whether the item is under legal hold.</summary>
    bool LegalHold,

    /// <summary>Content parts that make up the item body.</summary>
    MessageContent Content,

    /// <summary>Chain-of-custody provenance record.</summary>
    ChainOfCustody ChainOfCustody
);

/// <summary>Provenance information describing where the message originated.</summary>
public sealed record MessageSource(
    /// <summary>Source system label (always <c>EnterpriseVault</c> in this pipeline).</summary>
    string System,

    /// <summary>Original EV archive identifier.</summary>
    string ArchiveId,

    /// <summary>Original EV item identifier.</summary>
    string ItemId,

    /// <summary>Vault store the item was retrieved from.</summary>
    string VaultStore
);

/// <summary>Retention policy to be applied in storionX.</summary>
public sealed record RetentionPolicy(
    /// <summary>Retention category label carried over from EV.</summary>
    string Category,

    /// <summary>Absolute end-of-retention date, or <see langword="null"/> for indefinite.</summary>
    DateTime? ExpiresUtc
);

/// <summary>The content of an ingest message expressed as an ordered list of SIS parts.</summary>
public sealed record MessageContent(IReadOnlyList<MessagePart> Parts);

/// <summary>A single content part referencing a deduplicated SIS blob.</summary>
public sealed record MessagePart(
    /// <summary>Vault-scoped part identifier.</summary>
    string PartId,

    /// <summary>Hex-encoded SHA-256 hash of the part bytes.</summary>
    string Sha256,

    /// <summary>Size in bytes.</summary>
    long SizeBytes
);

/// <summary>Chain-of-custody record written at ingest time.</summary>
public sealed record ChainOfCustody(
    /// <summary>UTC time the item was extracted from EV.</summary>
    DateTime ExtractedAtUtc,

    /// <summary>Identifier of the migration run that processed this item.</summary>
    Guid RunId,

    /// <summary>Semantic version of the migration tooling.</summary>
    string ToolVersion
);
