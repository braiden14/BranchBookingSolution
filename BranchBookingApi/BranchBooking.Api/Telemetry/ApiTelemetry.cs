using System.Diagnostics.Metrics;

namespace BranchBooking.Api.Telemetry;

public static class ApiTelemetry
{
    public const string MeterName = "BranchBooking.Api";

    public static readonly Meter Meter = new(MeterName, "1.0.0");

    public static readonly Counter<long> Requests = Meter.CreateCounter<long>(
        "branchbooking.api.requests",
        unit: "{request}",
        description: "Number of HTTP requests processed by the API.");

    public static readonly Counter<long> Failures = Meter.CreateCounter<long>(
        "branchbooking.api.failures",
        unit: "{failure}",
        description: "Number of HTTP requests that returned an error status.");

    public static readonly Histogram<double> Duration = Meter.CreateHistogram<double>(
        "branchbooking.api.request.duration",
        unit: "ms",
        description: "HTTP request duration in milliseconds.");
}