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
    // Services registered in the DI container, the DbContext included, are injected as handler parameters.
    // The CancellationToken is bound to the request: it's cancelled if the client disconnects.
    internal static async Task<Ok<List<TechnologyDto>>> Handle(AppDbContext db, CancellationToken cancellationToken)
    {
        var items = await db.Set<Technology>()
            .OrderBy(item => item.Id)
            .Select(TechnologyDto.Projection)
            .ToListAsync(cancellationToken);

        return TypedResults.Ok(items);
    }
#else
    // Services registered in the DI container are injected as handler parameters.
    internal static Ok<List<TechnologyDto>> Handle(TechnologyStore store) =>
        TypedResults.Ok(store.GetAll().Select(TechnologyDto.From).ToList());
#endif
}
