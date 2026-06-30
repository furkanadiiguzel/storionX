using EvStorionX.Application.Dto;

namespace EvStorionX.Application.Abstractions;

/// <summary>HTTP client abstraction for the storionX ingest API.</summary>
public interface IStorionXClient
{
    /// <summary>
    /// Submits <paramref name="message"/> to storionX and returns the structured outcome.
    /// Never throws for expected HTTP error codes — caller inspects <see cref="IngestResult.Status"/>.
    /// </summary>
    Task<IngestResult> IngestAsync(StorionXMessage message, CancellationToken ct);

    /// <summary>
    /// Retrieves aggregated statistics from storionX <c>GET /stats</c>.
    /// Returns <see langword="null"/> when the request fails or the circuit is open.
    /// </summary>
    Task<StorionXStats?> GetStatsAsync(CancellationToken ct);
}
