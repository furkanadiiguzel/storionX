using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using EvStorionX.Application.Abstractions;
using EvStorionX.Application.Dto;

namespace EvStorionX.Infrastructure.StorionXClient;

/// <summary>
/// HTTP implementation of <see cref="IStorionXClient"/> that submits items to the storionX ingest API.
/// Retry and circuit-breaker resilience are wired by the DI registration — this class never
/// constructs <see cref="HttpClient"/> directly.
/// </summary>
public sealed partial class HttpStorionXClient(
    HttpClient httpClient,
    ILogger<HttpStorionXClient> logger) : IStorionXClient
{
    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNamingPolicy        = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition      = JsonIgnoreCondition.WhenWritingNull,
        PropertyNameCaseInsensitive = true,
    };

    /// <inheritdoc/>
    public async Task<IngestResult> IngestAsync(StorionXMessage message, CancellationToken ct)
    {
        HttpResponseMessage resp;

        try
        {
            resp = await httpClient.PostAsJsonAsync("/ingest", message, JsonOpts, ct);
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException
                                       or Polly.CircuitBreaker.BrokenCircuitException)
        {
            LogRequestFailed(logger, ex.GetType().Name, ex);
            return new IngestResult(IngestStatus.TransientError, null, null, ex.GetType().Name);
        }

        if (resp.IsSuccessStatusCode)
        {
            var body = await resp.Content.ReadFromJsonAsync<IngestResponse>(JsonOpts, ct);
            var status = body!.AlreadyPresent ? IngestStatus.AlreadyPresent : IngestStatus.Ingested;
            LogSuccess(logger, status, body.TargetId);
            return new IngestResult(status, body.TargetId, null, null);
        }

        // 429 / 503 reach here only when Polly has exhausted all retry attempts
        if (resp.StatusCode is HttpStatusCode.TooManyRequests or HttpStatusCode.ServiceUnavailable)
        {
            var retryAfter = resp.Headers.RetryAfter?.Delta;
            LogRetriesExhausted(logger, (int)resp.StatusCode, retryAfter);
            return new IngestResult(IngestStatus.TransientError, null, retryAfter, resp.StatusCode.ToString());
        }

        // 4xx permanent failure — no point retrying
        LogPermanentError(logger, (int)resp.StatusCode);
        return new IngestResult(IngestStatus.PermanentError, null, null, resp.StatusCode.ToString());
    }

    // ── LoggerMessage delegates ───────────────────────────────────────────────

    [LoggerMessage(Level = LogLevel.Information,
        Message = "storionX ingest succeeded: status={Status}, targetId={TargetId}")]
    private static partial void LogSuccess(ILogger logger, IngestStatus status, string targetId);

    [LoggerMessage(Level = LogLevel.Warning,
        Message = "storionX retries exhausted: httpStatus={HttpStatus}, retryAfter={RetryAfter}")]
    private static partial void LogRetriesExhausted(ILogger logger, int httpStatus, TimeSpan? retryAfter);

    [LoggerMessage(Level = LogLevel.Error,
        Message = "storionX permanent error: httpStatus={HttpStatus}")]
    private static partial void LogPermanentError(ILogger logger, int httpStatus);

    [LoggerMessage(Level = LogLevel.Error,
        Message = "storionX request failed with {ExceptionType}")]
    private static partial void LogRequestFailed(ILogger logger, string exceptionType, Exception ex);

    // ── Private response model (matches MockStorionX JSON shape) ─────────────

    private sealed record IngestResponse(string TargetId, bool Deduped, bool AlreadyPresent);
}
