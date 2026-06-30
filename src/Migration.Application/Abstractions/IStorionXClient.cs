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
}
