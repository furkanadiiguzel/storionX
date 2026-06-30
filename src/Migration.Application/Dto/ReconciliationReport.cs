namespace EvStorionX.Application.Dto;

/// <summary>
/// Result of cross-referencing local migration records against what storionX actually holds.
/// Discrepancies indicate items that may need re-ingestion or manual investigation.
/// </summary>
public sealed record ReconciliationReport(
    /// <summary>Run this report covers.</summary>
    Guid RunId,

    /// <summary>UTC time the reconciliation was performed.</summary>
    DateTime GeneratedAtUtc,

    /// <summary>Items recorded as migrated locally but not found in storionX.</summary>
    IReadOnlyList<string> MissingInTarget,

    /// <summary>Items found in storionX that do not match the local migration record.</summary>
    IReadOnlyList<string> MismatchedInTarget,

    /// <summary>Items that appear in storionX but have no corresponding local migration record.</summary>
    IReadOnlyList<string> UnexpectedInTarget,

    /// <summary><see langword="true"/> when all three discrepancy lists are empty.</summary>
    bool IsClean
);
