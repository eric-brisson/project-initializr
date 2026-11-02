using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace Starter.Template.Api.Common.Telemetry;

// The source of the custom spans and the meter of the custom metrics, both named after the API. They're plain .NET
// (System.Diagnostics): without a listener they cost next to nothing. The OpenTelemetry SDK (TelemetryRegistration)
// listens to them locally; in ARO, Dynatrace OneAgent can too, if it's set to capture .NET activity sources.
public static class AppTelemetry
{
    public static readonly string Name = typeof(AppTelemetry).Assembly.GetName().Name!;

    public static readonly ActivitySource Source = new(Name);

    public static readonly Meter Meter = new(Name);
}
