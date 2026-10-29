using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http.HttpResults;
#if (UseDatabase || UseInMemory)
using Microsoft.EntityFrameworkCore;
using Starter.Template.Api.Common.Data;
#endif

namespace Starter.Template.Api.Features.Technologies;

public static class AddTechnology
{
    public sealed record AddTechnologyRequest(
        [property: Required, StringLength(100)] string Name,
        [property: Required, StringLength(50)] string Version);

    public static void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("", Handle)
            .WithName(nameof(AddTechnology));
    }

#if (UseDatabase || UseInMemory)
    // The request is bound from the JSON body and validated before the handler runs
    // (see AddValidation in Program.cs): an invalid body gets a 400 problem details response.
    internal static async Task<Results<CreatedAtRoute<ListTechnologies.TechnologyDto>, ProblemHttpResult>> Handle(
        AddTechnologyRequest request,
        IBuiltInTechnologies builtIn,
        AppDbContext db,
        CancellationToken cancellationToken)
    {
        // Upper-cased on both sides, since SQL Server compares names case-insensitively by default and SQLite doesn't.
        // The unique index on Name still rejects a duplicate inserted by a concurrent request.
        var name = request.Name.ToUpperInvariant();
        if (builtIn.Contains(request.Name)
            || await db.Technologies.AnyAsync(technology => technology.Name.ToUpper() == name, cancellationToken))
        {
            return AlreadyExists(request.Name);
        }

        db.Technologies.Add(new Technology { Name = request.Name, Version = request.Version });
        await db.SaveChangesAsync(cancellationToken);

        return TypedResults.CreatedAtRoute(
            new ListTechnologies.TechnologyDto(request.Name, request.Version),
            nameof(ListTechnologies));
    }
#else
    // The request is bound from the JSON body and validated before the handler runs
    // (see AddValidation in Program.cs): an invalid body gets a 400 problem details response.
    internal static Results<CreatedAtRoute<ListTechnologies.TechnologyDto>, ProblemHttpResult> Handle(
        AddTechnologyRequest request,
        IBuiltInTechnologies builtIn,
        TechnologyStore store)
    {
        var technology = new ListTechnologies.TechnologyDto(request.Name, request.Version);
        if (builtIn.Contains(request.Name) || !store.TryAdd(technology))
        {
            return AlreadyExists(request.Name);
        }

        return TypedResults.CreatedAtRoute(technology, nameof(ListTechnologies));
    }
#endif

    private static ProblemHttpResult AlreadyExists(string name) =>
        TypedResults.Problem(
            statusCode: StatusCodes.Status409Conflict,
            title: "Technology already exists.",
            detail: $"A technology named '{name}' is already in the catalog.");
}
