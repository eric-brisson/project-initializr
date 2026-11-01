using Microsoft.AspNetCore.Diagnostics.HealthChecks;
#if (UseScalar)
using Scalar.AspNetCore;
#endif
using Starter.Template.Api.Common;
#if (UseEfCore)
using Starter.Template.Api.Common.Data;
#endif

namespace Starter.Template.Api;

public class Program
{
    // Health checks registered with this tag decide readiness, e.g. AddDbContextCheck<AppDbContext>(tags: [Program.ReadyTag]).
    public const string ReadyTag = "ready";

    public static void Main(string[] args)
    {
        // Configuration sources, each overriding the previous ones: appsettings.json, appsettings.{Environment}.json,
        // user secrets (Development only: dotnet user-secrets set <key> <value>), environment variables, command line.
        var builder = WebApplication.CreateBuilder(args);

        builder.Services.AddAuthorization();

        // Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
        builder.Services.AddOpenApi();

        // Error responses use the RFC 9457 problem details format: validation failures, error status codes
        // returned without a body (404...) and exceptions (see UseExceptionHandler below).
        builder.Services.AddProblemDetails();

        // Validates minimal API parameters annotated with data annotations ([Required], [StringLength], ...).
        builder.Services.AddValidation();

        // Probes for the orchestrator (Kubernetes...), mapped under /actuator/health below.
        builder.Services.AddHealthChecks();

        builder.Services.AddFeatures();
#if (UseEfCore)

        builder.AddDatabase();
#endif

        var app = builder.Build();

        // First in the pipeline, so it catches the exceptions of everything after it. The handlers registered with
        // AddExceptionHandler get the first try; otherwise the response is a 500 problem details, without the
        // exception (it's in the logs). In Development too, so the responses are the same as in Production.
        app.UseExceptionHandler();

        // Gives a problem details body to the error status codes returned without one (404, 405...).
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

        // Liveness: the process is up and answers. It runs no check, so a database outage
        // makes the app not ready (no traffic) instead of getting it restarted.
        app.MapHealthChecks("/actuator/health/liveness", new HealthCheckOptions { Predicate = _ => false });

        // Readiness: every check tagged "ready" passes, so the app can take traffic.
        app.MapHealthChecks("/actuator/health/readiness", new HealthCheckOptions
        {
            Predicate = check => check.Tags.Contains(ReadyTag),
        });

        app.MapEndpoints();

        app.Run();
    }
}
