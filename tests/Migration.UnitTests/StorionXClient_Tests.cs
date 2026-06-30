using System.Net;
using System.Text.Json;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Http.Resilience;
using Microsoft.Extensions.Options;
using Polly;
using EvStorionX.Application.Abstractions;
using EvStorionX.Application.Dto;
using EvStorionX.Infrastructure.StorionXClient;

namespace EvStorionX.UnitTests;

public sealed class StorionXClient_Tests
{
    // ── Helpers ───────────────────────────────────────────────────────────────

    private sealed class FakeHandler(Queue<HttpResponseMessage> queue) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage req, CancellationToken ct)
        {
            if (queue.Count == 0)
                throw new InvalidOperationException("FakeHandler queue exhausted — provide more responses");
            return Task.FromResult(queue.Dequeue());
        }
    }

    private static HttpResponseMessage IngestOk(bool alreadyPresent = false)
    {
        var body = JsonSerializer.Serialize(new
        {
            targetId = "tgt-1",
            deduped = alreadyPresent,
            alreadyPresent,
        });
        return new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(body, System.Text.Encoding.UTF8, "application/json"),
        };
    }

    private static HttpResponseMessage Status(HttpStatusCode code) => new(code);

    /// <summary>
    /// Builds a real <see cref="HttpStorionXClient"/> wired to a fake HTTP handler queue.
    /// Polly is configured with 0ms delays and a disabled circuit breaker so tests run instantly.
    /// </summary>
    private static IStorionXClient BuildSut(int maxRetryAttempts, params HttpResponseMessage[] responses)
    {
        var queue = new Queue<HttpResponseMessage>(responses);

        var services = new ServiceCollection();
        services.AddLogging();
        services.Configure<StorionXClientOptions>(o =>
        {
            o.BaseUrl = "http://test-host";
            o.MaxRetryAttempts = maxRetryAttempts;
            o.BaseDelayMs = 0;
            o.CircuitBreakerFailureRatio = 1.0;
            o.CircuitBreakerSamplingSeconds = 300;
        });

        services
            .AddHttpClient<IStorionXClient, HttpStorionXClient>(c =>
                c.BaseAddress = new Uri("http://test-host"))
            .ConfigurePrimaryHttpMessageHandler(() => new FakeHandler(queue))
            .AddStandardResilienceHandler()
            .Configure((HttpStandardResilienceOptions o, IServiceProvider sp) =>
            {
                var opts = sp.GetRequiredService<IOptions<StorionXClientOptions>>().Value;

                // Retry — same predicate as production, but 0ms delay so tests don't sleep
                o.Retry.MaxRetryAttempts = opts.MaxRetryAttempts;
                o.Retry.Delay = TimeSpan.Zero;
                o.Retry.BackoffType = DelayBackoffType.Constant;
                o.Retry.UseJitter = false;
                o.Retry.DelayGenerator = null;  // don't honour Retry-After in unit tests
                o.Retry.ShouldHandle = static args => ValueTask.FromResult(
                    args.Outcome.Result is { StatusCode: HttpStatusCode.TooManyRequests }
                                       or { StatusCode: HttpStatusCode.ServiceUnavailable }
                    || args.Outcome.Exception is HttpRequestException);

                // Circuit breaker — effectively disabled: requires 1000 calls to sample
                o.CircuitBreaker.MinimumThroughput = 1000;
                o.CircuitBreaker.SamplingDuration = TimeSpan.FromSeconds(300);
                o.CircuitBreaker.FailureRatio = 1.0;

                // Timeouts — generous so the fake handler never triggers them
                o.TotalRequestTimeout.Timeout = TimeSpan.FromSeconds(30);
                o.AttemptTimeout.Timeout = TimeSpan.FromSeconds(30);
            });

        return services.BuildServiceProvider().GetRequiredService<IStorionXClient>();
    }

    private static StorionXMessage AnyMessage() => new(
        IdempotencyKey: "ev:v:a:i",
        TargetArchive: "user_mailbox:alice@contoso.com",
        ArchiveClass: "user_mailbox",
        Source: new MessageSource("EnterpriseVault", "arch1", "item1", "vault1"),
        Metadata: new Dictionary<string, string>(),
        Retention: new RetentionPolicy("Standard-7Y", null),
        LegalHold: false,
        Content: new MessageContent([new MessagePart("p1", new string('a', 64), 1)]),
        ChainOfCustody: new ChainOfCustody(DateTime.UtcNow, Guid.NewGuid(), "1.0.0")
    );

    // ── Tests ─────────────────────────────────────────────────────────────────

    [Fact]
    public async Task IngestAsync_TwoTransient503ThenSuccess_ReturnsIngested()
    {
        // 2 retries (503, 503) then success on 3rd attempt
        var sut = BuildSut(maxRetryAttempts: 3,
            Status(HttpStatusCode.ServiceUnavailable),
            Status(HttpStatusCode.ServiceUnavailable),
            IngestOk());

        var result = await sut.IngestAsync(AnyMessage(), CancellationToken.None);

        result.Status.Should().Be(IngestStatus.Ingested);
        result.TargetId.Should().Be("tgt-1");
    }

    [Fact]
    public async Task IngestAsync_RetriesExhaustedOn429_ReturnsTransientErrorWithoutThrowing()
    {
        // MaxRetryAttempts=5 → 6 total calls (1 original + 5 retries), all return 429
        var sut = BuildSut(maxRetryAttempts: 5,
            Status(HttpStatusCode.TooManyRequests),
            Status(HttpStatusCode.TooManyRequests),
            Status(HttpStatusCode.TooManyRequests),
            Status(HttpStatusCode.TooManyRequests),
            Status(HttpStatusCode.TooManyRequests),
            Status(HttpStatusCode.TooManyRequests));

        var act = () => sut.IngestAsync(AnyMessage(), CancellationToken.None);
        var result = await act.Should().NotThrowAsync();

        result.Which.Status.Should().Be(IngestStatus.TransientError);
    }

    [Fact]
    public async Task IngestAsync_AlreadyPresentResponse_ReturnsAlreadyPresent()
    {
        var sut = BuildSut(maxRetryAttempts: 1, IngestOk(alreadyPresent: true));

        var result = await sut.IngestAsync(AnyMessage(), CancellationToken.None);

        result.Status.Should().Be(IngestStatus.AlreadyPresent);
    }

    [Fact]
    public async Task IngestAsync_UnprocessableEntity422_ReturnsPermanentErrorWithoutThrowing()
    {
        var sut = BuildSut(maxRetryAttempts: 1, Status(HttpStatusCode.UnprocessableEntity));

        var act = () => sut.IngestAsync(AnyMessage(), CancellationToken.None);
        var result = await act.Should().NotThrowAsync();

        result.Which.Status.Should().Be(IngestStatus.PermanentError);
    }
}
