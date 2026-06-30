namespace EvStorionX.Application.Dto;

/// <summary>Aggregated statistics returned by the storionX <c>GET /stats</c> endpoint.</summary>
public sealed record StorionXStats(
    long TotalIngested,
    int UniqueParts,
    long DedupedParts,
    IReadOnlyDictionary<string, long> ByTargetArchive,
    long Rejected429Count,
    long Transient503Count
);
