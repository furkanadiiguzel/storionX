using System.Net;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Http.Resilience;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Polly;
using EvStorionX.Application.Abstractions;

namespace EvStorionX.Infrastructure.StorionXClient;

/// <summary>Registers <see cref="HttpStorionXClient"/> with its full resilience pipeline.</summary>
public static partial class StorionXClientDependencyInjection
{
    /// <summary>
    /// Adds a typed <see cref="IStorionXClient"/> backed by <see cref="HttpStorionXClient"/>
    /// with exponential-backoff retry (honouring <c>Retry-After</c>) and circuit breaker.
    /// All policy parameters are read from <see cref="StorionXClientOptions"/> so they can be
    /// changed without recompiling.
    /// </summary>
    public static IServiceCollection AddStorionXClient(this IServiceCollection services)
    {
        services
            .AddHttpClient<IStorionXClient, HttpStorionXClient>((sp, c) =>
            {
                var opts = sp.GetRequiredService<IOptions<StorionXClientOptions>>().Value;
                c.BaseAddress = new Uri(opts.BaseUrl);
                c.Timeout     = TimeSpan.FromSeconds(30);
            })
            .AddStandardResilienceHandler()
            .Configure((HttpStandardResilienceOptions o, IServiceProvider sp) =>
            {
                var opts   = sp.GetRequiredService<IOptions<StorionXClientOptions>>().Value;
                var logger = sp.GetRequiredService<ILoggerFactory>()
                               .CreateLogger<HttpStorionXClient>();

                // ── Retry ────────────────────────────────────────────────────
                o.Retry.MaxRetryAttempts = opts.MaxRetryAttempts;
                o.Retry.BackoffType      = DelayBackoffType.Exponential;
                o.Retry.UseJitter        = true;
                o.Retry.Delay            = TimeSpan.FromMilliseconds(opts.BaseDelayMs);

                o.Retry.ShouldHandle = args => ValueTask.FromResult(
                    args.Outcome.Result is
                        { StatusCode: HttpStatusCode.TooManyRequests } or
                        { StatusCode: HttpStatusCode.ServiceUnavailable }
                    || args.Outcome.Exception is HttpRequestException);

                // Honour Retry-After header when present; fall back to exponential+jitter.
                o.Retry.DelayGenerator = static args =>
                {
                    if (args.Outcome.Result?.Headers.RetryAfter is { Delta: { } delta })
                        return ValueTask.FromResult<TimeSpan?>(delta);
                    return ValueTask.FromResult<TimeSpan?>(null);
                };

                o.Retry.OnRetry = args =>
                {
                    LogRetry(logger,
                        args.AttemptNumber + 1,
                        opts.MaxRetryAttempts,
                        (int?)args.Outcome.Result?.StatusCode,
                        args.RetryDelay,
                        args.Outcome.Exception);
                    return default;
                };

                // ── Circuit breaker ───────────────────────────────────────────
                o.CircuitBreaker.FailureRatio    = opts.CircuitBreakerFailureRatio;
                o.CircuitBreaker.SamplingDuration = TimeSpan.FromSeconds(opts.CircuitBreakerSamplingSeconds);
            });

        return services;
    }

    [LoggerMessage(Level = LogLevel.Warning,
        Message = "storionX retry {Attempt}/{Max}: httpStatus={HttpStatus}, nextDelay={Delay}")]
    private static partial void LogRetry(
        ILogger logger, int attempt, int max, int? httpStatus, TimeSpan delay, Exception? exception);
}
