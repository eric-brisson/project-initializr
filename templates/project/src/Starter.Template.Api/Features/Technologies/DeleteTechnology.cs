using Microsoft.AspNetCore.Http.HttpResults;
#if (UseEfCore)
using Microsoft.EntityFrameworkCore;
using Starter.Template.Api.Common.Data;
#endif

namespace Starter.Template.Api.Features.Technologies;

public static class DeleteTechnology
{
    public static void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapDelete("{id:int}", Handle)
            .WithName(nameof(DeleteTechnology));
    }

#if (UseEfCore)
    internal static async Task<Results<NoContent, NotFound>> Handle(
        int id,
        AppDbContext db,
        CancellationToken cancellationToken)
    {
        // Une seule instruction DELETE, sans charger l'élément d'abord.
        var deleted = await db.Set<Technology>()
            .Where(item => item.Id == id)
            .ExecuteDeleteAsync(cancellationToken);

        return deleted == 0 ? TypedResults.NotFound() : TypedResults.NoContent();
    }
#else
    internal static Results<NoContent, NotFound> Handle(int id, TechnologyStore store) =>
        store.Remove(id) ? TypedResults.NoContent() : TypedResults.NotFound();
#endif
}
