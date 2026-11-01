using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http.HttpResults;
#if (UseEfCore)
using Starter.Template.Api.Common.Data;
#endif

namespace Starter.Template.Api.Features.Technologies;

public static class UpdateTechnology
{
    //#if (IsFeature)
    // TODO: add the other properties a client can change, with their validation attributes.
    //#endif
    public sealed record UpdateTechnologyRequest([property: Required, StringLength(100)] string Name);

    public static void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPut("{id:int}", Handle)
            .WithName(nameof(UpdateTechnology))
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status409Conflict);
    }

#if (UseEfCore)
    internal static async Task<Results<NoContent, NotFound>> Handle(
        int id,
        UpdateTechnologyRequest request,
        AppDbContext db,
        CancellationToken cancellationToken)
    {
        // Tracked: EF Core saves only the properties that changed.
        var item = await db.Set<Technology>().FindAsync([id], cancellationToken);
        if (item is null)
        {
            return TypedResults.NotFound();
        }

        item.Name = request.Name;

        // A name taken by another one breaks the unique index: UniqueConstraintExceptionHandler answers 409 Conflict.
        await db.SaveChangesAsync(cancellationToken);

        return TypedResults.NoContent();
    }
#else
    internal static Results<NoContent, NotFound, ProblemHttpResult> Handle(
        int id,
        UpdateTechnologyRequest request,
        TechnologyStore store) =>
        store.TryReplace(new Technology { Id = id, Name = request.Name }) switch
        {
            TechnologyStore.ReplaceResult.Replaced => TypedResults.NoContent(),
            TechnologyStore.ReplaceResult.NotFound => TypedResults.NotFound(),
            _ => TypedResults.Problem(statusCode: StatusCodes.Status409Conflict, detail: $"The name '{request.Name}' is already taken."),
        };
#endif
}
