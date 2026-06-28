using System.Text.Json;

namespace EvStorionX.MockStorionX.Models;

/// <summary>Payload for <c>POST /ingest</c>.</summary>
public sealed record IngestRequest(
    /// <summary>Stable content-addressed key — format <c>ev:&lt;vault&gt;:&lt;archive&gt;:&lt;item&gt;</c>.</summary>
    string IdempotencyKey,
    /// <summary>storionX target archive identifier (e.g. <c>user_mailbox:ayse@contoso.com</c>).</summary>
    string TargetArchive,
    /// <summary>Logical archive class label.</summary>
    string ArchiveClass,
    /// <summary>Arbitrary item metadata (passed through as-is).</summary>
    JsonElement? Metadata,
    /// <summary>Retention policy blob (passed through as-is).</summary>
    JsonElement? Retention,
    /// <summary>Whether the item is under a legal hold.</summary>
    bool LegalHold,
    /// <summary>Content parts that make up this item.</summary>
    IngestContent Content
);

/// <summary>Content envelope containing one or more SIS parts.</summary>
public sealed record IngestContent(
    /// <summary>Ordered list of content parts to deduplicate and store.</summary>
    IReadOnlyList<ContentPart> Parts
);

/// <summary>A single SIS content part reference.</summary>
public sealed record ContentPart(
    /// <summary>Vault-assigned part identifier.</summary>
    string PartId,
    /// <summary>Hex-encoded SHA-256 digest — used as the dedup key.</summary>
    string Sha256,
    /// <summary>Raw size of the part in bytes.</summary>
    long SizeBytes
);
