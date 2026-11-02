using System.ComponentModel.DataAnnotations;
#if (UseTelemetry)
using System.Diagnostics.Metrics;
#endif
using Microsoft.AspNetCore.Http.HttpResults;
#if (UseEfCore)
using Starter.Template.Api.Common.Data;
#endif
#if (UseTelemetry)
using Starter.Template.Api.Common.Telemetry;
#endif

namespace Starter.Template.Api.Features.Technologies;

public static class CreateTechnology
{
    //#if (IsFeature)
    // TODO: add the other properties a client sends to create a Technology, with their validation attributes.
    //#endif
    public sealed record CreateTechnologyRequest([property: Required, StringLength(100)] string Name);

#if (UseTelemetry)
    // A custom metric, next to the ones ASP.NET Core records by itself (Metrics page of the dashboard).
    private static readonly Counter<long> Created =
        AppTelemetry.Meter.CreateCounter<long>("technologies.created", description: "Technologies created through the API");

#endif
    public static void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("", Handle)
            .WithName(nameof(CreateTechnology))
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status409Conflict);
    }

    // The request is bound from the JSON body and validated before the handler runs
    // (AddValidation in Program.cs): an invalid body gets a 400 problem details response.
#if (UseEfCore)
    internal static async Task<CreatedAtRoute<TechnologyDto>> Handle(
        CreateTechnologyRequest request,
        AppDbContext db,
        CancellationToken cancellationToken)
    {
#if (UseTelemetry)
        // A custom span, inside the request span of ASP.NET Core: it shows in the trace with its tags.
        using var activity = AppTelemetry.Source.StartActivity("CreateTechnology");
        activity?.SetTag("technology.name", request.Name);

#endif
        var item = new Technology { Name = request.Name };
        db.Set<Technology>().Add(item);

        // A duplicate name breaks the unique index: UniqueConstraintExceptionHandler answers 409 Conflict.
        // No check beforehand: two requests could both pass it, only the database settles the race.
        await db.SaveChangesAsync(cancellationToken);
#if (UseTelemetry)

        activity?.SetTag("technology.id", item.Id);
        Created.Add(1);
#endif

        return TypedResults.CreatedAtRoute(TechnologyDto.From(item), nameof(GetTechnology), new { id = item.Id });
    }
#else
    internal static Results<CreatedAtRoute<TechnologyDto>, ProblemHttpResult> Handle(
        CreateTechnologyRequest request,
        TechnologyStore store)
    {
#if (UseTelemetry)
        // A custom span, inside the request span of ASP.NET Core: it shows in the trace with its tags.
        using var activity = AppTelemetry.Source.StartActivity("CreateTechnology");
        activity?.SetTag("technology.name", request.Name);

#endif
        var item = new Technology { Name = request.Name };
        if (!store.TryAdd(item))
        {
            return TypedResults.Problem(statusCode: StatusCodes.Status409Conflict, detail: $"The name '{request.Name}' is already taken.");
        }
#if (UseTelemetry)

        activity?.SetTag("technology.id", item.Id);
        Created.Add(1);
#endif

        return TypedResults.CreatedAtRoute(TechnologyDto.From(item), nameof(GetTechnology), new { id = item.Id });
    }
#endif
}
