namespace EvStorionX.MockStorionX.Extensions;

/// <summary>Custom <see cref="IResult"/> factories that add <c>Retry-After</c> headers.</summary>
public static class ResultsExtensions
{
    /// <summary>Returns HTTP 429 with a <c>Retry-After</c> header.</summary>
    public static IResult TooManyRequests(int retryAfterSeconds = 1) =>
        new RetryAfterResult(StatusCodes.Status429TooManyRequests, retryAfterSeconds);

    /// <summary>Returns HTTP 503 with a <c>Retry-After</c> header.</summary>
    public static IResult ServiceUnavailable(int retryAfterSeconds = 2) =>
        new RetryAfterResult(StatusCodes.Status503ServiceUnavailable, retryAfterSeconds);

    private sealed class RetryAfterResult(int statusCode, int retryAfterSeconds) : IResult
    {
        public Task ExecuteAsync(HttpContext httpContext)
        {
            httpContext.Response.StatusCode = statusCode;
            httpContext.Response.Headers["Retry-After"] = retryAfterSeconds.ToString(System.Globalization.CultureInfo.InvariantCulture);
            return Task.CompletedTask;
        }
    }
}
