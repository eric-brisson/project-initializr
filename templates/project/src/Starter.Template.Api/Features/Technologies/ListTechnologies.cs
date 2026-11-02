using Microsoft.AspNetCore.Http.HttpResults;
#if (UseEfCore)
using Microsoft.EntityFrameworkCore;
using Starter.Template.Api.Common.Data;
#endif

namespace Starter.Template.Api.Features.Technologies;

public static class ListTechnologies
{
    public static void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("", Handle)
            .WithName(nameof(ListTechnologies));
    }

#if (UseEfCore)
    // Les services enregistrés dans le conteneur d'injection, le DbContext compris, arrivent en paramètres du handler.
    // Le CancellationToken est lié à la requête : il est annulé si le client se déconnecte.
    internal static async Task<Ok<List<TechnologyDto>>> Handle(AppDbContext db, CancellationToken cancellationToken)
    {
        // AsNoTracking : lecture seule, EF Core n'a pas besoin de surveiller les changements de ces entités.
        var items = await db.Set<Technology>()
            .AsNoTracking()
            .OrderBy(item => item.Id)
            .ToListAsync(cancellationToken);

        return TypedResults.Ok(items.Select(TechnologyDto.From).ToList());
    }
#else
    // Les services enregistrés dans le conteneur d'injection arrivent en paramètres du handler.
    internal static Ok<List<TechnologyDto>> Handle(TechnologyStore store) =>
        TypedResults.Ok(store.GetAll().Select(TechnologyDto.From).ToList());
#endif
}
