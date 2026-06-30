namespace EvStorionX.Application.Dto;

/// <summary>
/// Scoping filters that constrain which items are eligible for migration in a given run.
/// All filters are optional; unset filters match everything.
/// </summary>
public sealed record MigrationFilters(
    /// <summary>Only migrate items sent on or after this date (inclusive).</summary>
    DateTime? FromUtc = null,

    /// <summary>Only migrate items sent before this date (exclusive).</summary>
    DateTime? ToUtc = null,

    /// <summary>Restrict migration to a single EV archive; <see langword="null"/> means all archives.</summary>
    string? ArchiveId = null,

    /// <summary>Only migrate items whose <c>FolderPath</c> starts with this prefix.</summary>
    string? FolderPath = null,

    /// <summary>Only migrate items with this EV retention category label.</summary>
    string? RetentionCategory = null
);
