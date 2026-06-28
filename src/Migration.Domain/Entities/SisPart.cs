namespace EvStorionX.Domain.Entities;

/// <summary>
/// A Single-Instance Storage part — a deduplicated content chunk stored in the EV vault.
/// Multiple <see cref="Item"/> records may reference the same <see cref="SisPart"/>.
/// </summary>
public sealed class SisPart
{
    /// <summary>Unique identifier for this SIS part within the vault store.</summary>
    public required string PartId { get; init; }

    /// <summary>Hex-encoded SHA-256 hash of the raw part bytes; used for integrity verification.</summary>
    public required string Sha256 { get; init; }

    /// <summary>Size of the part in bytes.</summary>
    public required long SizeBytes { get; init; }

    /// <summary>Storage reference — blob path or URI where the raw bytes can be fetched.</summary>
    public required string DataRef { get; init; }
}
