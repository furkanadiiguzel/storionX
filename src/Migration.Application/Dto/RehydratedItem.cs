namespace EvStorionX.Application.Dto;

/// <summary>The fully assembled content of an EV item after its SIS parts have been fetched and combined.</summary>
public sealed record RehydratedItem(
    /// <summary>EV item identifier this content belongs to.</summary>
    string ItemId,

    /// <summary>Ordered list of content parts with their raw bytes and metadata.</summary>
    IReadOnlyList<RehydratedPart> Parts,

    /// <summary>UTC time the rehydration completed.</summary>
    DateTime RehydratedAtUtc
);

/// <summary>A single rehydrated SIS part.</summary>
public sealed record RehydratedPart(
    /// <summary>Vault-scoped part identifier.</summary>
    string PartId,

    /// <summary>Hex-encoded SHA-256 hash of <see cref="Bytes"/>.</summary>
    string Sha256,

    /// <summary>Size of the part in bytes.</summary>
    long SizeBytes,

    /// <summary>Raw part bytes.</summary>
    ReadOnlyMemory<byte> Bytes
);
