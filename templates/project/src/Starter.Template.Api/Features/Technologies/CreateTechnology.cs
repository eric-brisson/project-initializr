using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http.HttpResults;
#if (UseEfCore)
using Starter.Template.Api.Common.Data;
#endif

namespace Starter.Template.Api.Features.Technologies;

public static class CreateTechnology
{
    //#if (IsFeature)
    // TODO : ajoutez les autres propriétés qu'un client envoie à la création, avec leurs attributs de validation.
    //#endif
    public sealed record CreateTechnologyRequest([property: Required, StringLength(100)] string Name);

    public static void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("", Handle)
            .WithName(nameof(CreateTechnology))
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status409Conflict);
    }

    // La requête est lue depuis le corps JSON et validée avant l'appel du handler
    // (AddValidation dans Program.cs) : un corps invalide reçoit une réponse problem details 400.
#if (UseEfCore)
    internal static async Task<CreatedAtRoute<TechnologyDto>> Handle(
        CreateTechnologyRequest request,
        AppDbContext db,
        CancellationToken cancellationToken)
    {
        var item = new Technology { Name = request.Name };
        db.Set<Technology>().Add(item);

        // Un nom en double viole l'index unique : UniqueConstraintExceptionHandler répond 409 Conflict.
        // Aucune vérification avant : deux requêtes pourraient la passer toutes les deux, seule la base tranche.
        await db.SaveChangesAsync(cancellationToken);

        return TypedResults.CreatedAtRoute(TechnologyDto.From(item), nameof(GetTechnology), new { id = item.Id });
    }
#else
    internal static Results<CreatedAtRoute<TechnologyDto>, ProblemHttpResult> Handle(
        CreateTechnologyRequest request,
        TechnologyStore store)
    {
        var item = new Technology { Name = request.Name };
        if (!store.TryAdd(item))
        {
            return TypedResults.Problem(statusCode: StatusCodes.Status409Conflict, detail: $"The name '{request.Name}' is already taken.");
        }

        return TypedResults.CreatedAtRoute(TechnologyDto.From(item), nameof(GetTechnology), new { id = item.Id });
    }
#endif
}
