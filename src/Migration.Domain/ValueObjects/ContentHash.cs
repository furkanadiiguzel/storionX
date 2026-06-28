using System.Security.Cryptography;

namespace EvStorionX.Domain.ValueObjects;

/// <summary>
/// Wraps a SHA-256 digest for content integrity verification of SIS parts and migrated items.
/// </summary>
public readonly record struct ContentHash
{
    /// <summary>Lower-case hex representation of the 32-byte SHA-256 digest.</summary>
    public string Hex { get; }

    private ContentHash(string hex) => Hex = hex;

    /// <summary>Computes a <see cref="ContentHash"/> by hashing <paramref name="data"/> with SHA-256.</summary>
    public static ContentHash FromBytes(ReadOnlySpan<byte> data)
    {
        Span<byte> digest = stackalloc byte[SHA256.HashSizeInBytes];
        SHA256.HashData(data, digest);
        return new ContentHash(Convert.ToHexStringLower(digest));
    }

    /// <summary>Parses a hex-encoded SHA-256 string into a <see cref="ContentHash"/>.</summary>
    /// <exception cref="FormatException">Thrown when the string is not a valid 64-character hex digest.</exception>
    public static ContentHash Parse(string hex)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(hex);
        if (hex.Length != 64)
            throw new FormatException($"SHA-256 hex digest must be 64 characters; got {hex.Length}.");
        return new ContentHash(hex.ToLowerInvariant());
    }

    /// <inheritdoc/>
    public override string ToString() => Hex;
}
