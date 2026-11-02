using OpenTelemetry;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

namespace Starter.Template.Api.Common.Telemetry;

public static class TelemetryRegistration
{
    // The standard OpenTelemetry variable: where to send the traces, metrics and logs (OTLP). The launch profiles set it
    // to the local Aspire dashboard (docker compose up -d dashboard), and compose.yaml for the API container.
    // Nowhere else, so nothing is registered when deployed: in ARO, Dynatrace OneAgent instruments the API by itself,
    // and both together would record everything twice.
    public const string EndpointVariable = "OTEL_EXPORTER_OTLP_ENDPOINT";

    public static WebApplicationBuilder AddTelemetry(this WebApplicationBuilder builder)
    {
        if (string.IsNullOrWhiteSpace(builder.Configuration[EndpointVariable]))
        {
            return builder;
        }

        builder.Services.AddOpenTelemetry()
            .ConfigureResource(resource => resource.AddService(builder.Environment.ApplicationName))
            .WithTracing(tracing =>
            {
                tracing
                    .AddSource(AppTelemetry.Name)
                    // Only the API routes: the requests of Scalar, Swagger UI or the health probes would bury them.
                    .AddAspNetCoreInstrumentation(options => options.Filter = context => context.Request.Path.StartsWithSegments("/api"))
                    .AddHttpClientInstrumentation();
#if (UseDatabase)

                // The SQL queries, the ones of EF Core included, which goes through SqlClient.
                tracing.AddSqlClientInstrumentation();
#endif
            })
            .WithMetrics(metrics => metrics
                .AddMeter(AppTelemetry.Name)
                .AddAspNetCoreInstrumentation()
                .AddHttpClientInstrumentation()
                .AddRuntimeInstrumentation())
            // The logs too, each one linked to the span it was written in.
            .WithLogging(configureBuilder: null, configureOptions: options =>
            {
                options.IncludeScopes = true;
                options.IncludeFormattedMessage = true;
            })
            // One exporter for the three, configured by the OTEL_EXPORTER_OTLP_* variables.
            .UseOtlpExporter();

        return builder;
    }
}
