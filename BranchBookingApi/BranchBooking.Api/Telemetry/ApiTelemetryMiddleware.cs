using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace BranchBooking.Api.Telemetry;

public sealed class ApiTelemetryMiddleware(RequestDelegate next, ILogger<ApiTelemetryMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context)
    {
        var stopwatch = Stopwatch.StartNew();

        try
        {
            await next(context);
        }
        finally
        {
            stopwatch.Stop();
            var route = context.GetEndpoint()?.DisplayName ?? context.Request.Path.Value ?? "unknown";
            var tags = new TagList
            {
                { "http.request.method", context.Request.Method },
                { "http.response.status_code", context.Response.StatusCode },
                { "url.route", route }
            };

            ApiTelemetry.Requests.Add(1, tags);
            ApiTelemetry.Duration.Record(stopwatch.Elapsed.TotalMilliseconds, tags);
            if (context.Response.StatusCode >= 400)
            {
                ApiTelemetry.Failures.Add(1, tags);
            }

            logger.LogDebug(
                "HTTP request completed with status {StatusCode} in {DurationMilliseconds} ms",
                context.Response.StatusCode,
                stopwatch.Elapsed.TotalMilliseconds);
        }
    }
}