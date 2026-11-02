using Microsoft.AspNetCore.Diagnostics.HealthChecks;
#if (UseScalar)
using Scalar.AspNetCore;
#endif
using Starter.Template.Api.Common;
#if (UseEfCore)
using Starter.Template.Api.Common.Data;
#endif
#if (UseTelemetry)
using Starter.Template.Api.Common.Telemetry;
#endif

namespace Starter.Template.Api;

public class Program
{
    // Les vérifications de santé enregistrées avec ce tag décident de la disponibilité, par exemple AddDbContextCheck<AppDbContext>(tags: [Program.ReadyTag]).
    public const string ReadyTag = "ready";

    public static void Main(string[] args)
    {
        // Sources de configuration, chacune remplaçant les précédentes : appsettings.json, appsettings.{Environment}.json,
        // secrets utilisateur (Development seulement : dotnet user-secrets set <clé> <valeur>), variables d'environnement, ligne de commande.
        var builder = WebApplication.CreateBuilder(args);

        builder.Services.AddAuthorization();

        // Pour en savoir plus sur la configuration d'OpenAPI : https://aka.ms/aspnet/openapi
        builder.Services.AddOpenApi();

        // Les réponses d'erreur suivent le format problem details de la RFC 9457 : échecs de validation, codes d'erreur
        // renvoyés sans corps (404...) et exceptions (voir UseExceptionHandler plus bas).
        builder.Services.AddProblemDetails();

        // Valide les paramètres des minimal APIs annotés avec des data annotations ([Required], [StringLength], ...).
        builder.Services.AddValidation();

        // Sondes pour l'orchestrateur (Kubernetes...), mappées sous /actuator/health plus bas.
        builder.Services.AddHealthChecks();

        builder.Services.AddFeatures();
#if (UseTelemetry)

        // Traces, métriques et logs pour le tableau de bord Aspire local, seulement quand son adresse est définie (launchSettings.json).
        builder.AddTelemetry();
#endif
#if (UseEfCore)

        builder.AddDatabase();
#endif

        var app = builder.Build();

        // En premier dans le pipeline, pour attraper les exceptions de tout ce qui suit. Les gestionnaires enregistrés avec
        // AddExceptionHandler passent d'abord ; sinon la réponse est un problem details 500, sans l'exception (elle est
        // dans les logs). En Development aussi, pour que les réponses soient les mêmes qu'en Production.
        app.UseExceptionHandler();

        // Donne un corps problem details aux codes d'erreur renvoyés sans corps (404, 405...).
        app.UseStatusCodePages();
#if (UseInMemory)

        app.CreateDatabase();
#endif

        if (app.Environment.IsDevelopment())
        {
            app.MapOpenApi();
#if (UseScalar)
            app.MapScalarApiReference();
#endif
#if (UseSwaggerUi)
            app.UseSwaggerUI(options => options.SwaggerEndpoint("/openapi/v1.json", "v1"));
#endif
        }

        app.UseHttpsRedirection();

        app.UseAuthorization();

        // Vivacité : le processus tourne et répond. Aucune vérification : une panne de la base rend l'application
        // non disponible (plus de trafic) au lieu de la faire redémarrer.
        app.MapHealthChecks("/actuator/health/liveness", new HealthCheckOptions { Predicate = _ => false });

        // Disponibilité : toutes les vérifications marquées « ready » passent, l'application peut recevoir du trafic.
        app.MapHealthChecks("/actuator/health/readiness", new HealthCheckOptions
        {
            Predicate = check => check.Tags.Contains(ReadyTag),
        });

        app.MapEndpoints();

        app.Run();
    }
}
