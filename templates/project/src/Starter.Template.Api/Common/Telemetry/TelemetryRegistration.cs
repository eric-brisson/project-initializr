using OpenTelemetry;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

namespace Starter.Template.Api.Common.Telemetry;

public static class TelemetryRegistration
{
    // La variable standard d'OpenTelemetry : où envoyer les traces, les métriques et les logs (OTLP). Les profils de
    // lancement la règlent vers le tableau de bord Aspire local (docker compose up -d dashboard), et compose.yaml pour le
    // conteneur de l'API. Nulle part ailleurs : une fois déployée, rien n'est enregistré. Dans ARO, Dynatrace OneAgent
    // instrumente l'API de lui-même, et les deux ensemble enregistreraient tout en double.
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
                    // Seulement les routes de l'API : les requêtes de Scalar, de Swagger UI ou des sondes les noieraient.
                    .AddAspNetCoreInstrumentation(options => options.Filter = context => context.Request.Path.StartsWithSegments("/api"))
                    .AddHttpClientInstrumentation();
#if (UseDatabase)

                // Les requêtes SQL, y compris celles d'EF Core, qui passe par SqlClient.
                tracing.AddSqlClientInstrumentation();
#endif
            })
            .WithMetrics(metrics => metrics
                .AddMeter(AppTelemetry.Name)
                .AddAspNetCoreInstrumentation()
                .AddHttpClientInstrumentation()
                .AddRuntimeInstrumentation())
            // Les logs aussi, chacun relié au span pendant lequel il a été écrit.
            .WithLogging(configureBuilder: null, configureOptions: options =>
            {
                options.IncludeScopes = true;
                options.IncludeFormattedMessage = true;
            })
            // Un seul exportateur pour les trois, configuré par les variables OTEL_EXPORTER_OTLP_*.
            .UseOtlpExporter();

        // Démarre le tableau de bord local quand l'API est lancée depuis un profil de lancement (F5, dotnet run).
        if (builder.Environment.IsDevelopment() && builder.Configuration.GetValue<bool>(DashboardLauncher.EnabledKey))
        {
            builder.Services.AddHostedService<DashboardLauncher>();
        }

        return builder;
    }
}
