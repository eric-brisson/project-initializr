using Microsoft.AspNetCore.Http.HttpResults;
#if (UseEfCore)
using Microsoft.EntityFrameworkCore;
using Starter.Template.Api.Common.Data;
#endif

namespace Starter.Template.Api.Features.Technologies;

public static class GetTechnology
{
    public static void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("{id:int}", Handle)
            .WithName(nameof(GetTechnology));
    }

    // The 404 gets a problem details body from UseStatusCodePages (Program.cs).
#if (UseEfCore)
    internal static async Task<Results<Ok<TechnologyDto>, NotFound>> Handle(
        int id,
        AppDbContext db,
        CancellationToken cancellationToken)
    {
        var item = await db.Set<Technology>()
            .Where(item => item.Id == id)
            .Select(TechnologyDto.Projection)
            .FirstOrDefaultAsync(cancellationToken);

        return item is null ? TypedResults.NotFound() : TypedResults.Ok(item);
    }
#else
    internal static Results<Ok<TechnologyDto>, NotFound> Handle(int id, TechnologyStore store) =>
        store.Find(id) is { } item ? TypedResults.Ok(TechnologyDto.From(item)) : TypedResults.NotFound();
#endif
}
