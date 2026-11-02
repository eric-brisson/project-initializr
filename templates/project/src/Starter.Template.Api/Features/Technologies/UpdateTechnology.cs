using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http.HttpResults;
#if (UseEfCore)
using Starter.Template.Api.Common.Data;
#endif

namespace Starter.Template.Api.Features.Technologies;

public static class UpdateTechnology
{
    //#if (IsFeature)
    // TODO : ajoutez les autres propriétés qu'un client peut modifier, avec leurs attributs de validation.
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
        // Suivi par EF Core : il n'enregistre que les propriétés modifiées.
        var item = await db.Set<Technology>().FindAsync([id], cancellationToken);
        if (item is null)
        {
            return TypedResults.NotFound();
        }

        item.Name = request.Name;

        // Un nom déjà pris par un autre élément viole l'index unique : UniqueConstraintExceptionHandler répond 409 Conflict.
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
