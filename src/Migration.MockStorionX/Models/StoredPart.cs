namespace EvStorionX.MockStorionX.Models;

/// <summary>A content part that has been committed to the in-memory SIS store.</summary>
public sealed record StoredPart(
    string PartId,
    string Sha256,
    long SizeBytes,
    DateTime StoredAtUtc
);
