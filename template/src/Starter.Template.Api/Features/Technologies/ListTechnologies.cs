using Microsoft.AspNetCore.Http.HttpResults;
#if (UseDatabase || UseInMemory)
using Microsoft.EntityFrameworkCore;
using Starter.Template.Api.Common.Data;
#endif

namespace Starter.Template.Api.Features.Technologies;

public static class ListTechnologies
{
    public sealed record TechnologyDto(string Name, string Version);

    public static void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("", Handle)
            .WithName(nameof(ListTechnologies));
    }

#if (UseDatabase || UseInMemory)
    // Services registered in the DI container, the DbContext included, are injected as handler parameters.
    // The CancellationToken is bound to the request: it's cancelled if the client disconnects.
    internal static async Task<Ok<IReadOnlyList<TechnologyDto>>> Handle(
        IBuiltInTechnologies builtIn,
        AppDbContext db,
        CancellationToken cancellationToken)
    {
        // Projected straight to the DTO: EF Core reads only these columns and doesn't track anything.
        var added = await db.Technologies
            .OrderBy(technology => technology.Id)
            .Select(technology => new TechnologyDto(technology.Name, technology.Version))
            .ToListAsync(cancellationToken);

        return TypedResults.Ok<IReadOnlyList<TechnologyDto>>([.. builtIn.All, .. added]);
    }
#else
    // Services registered in the DI container are injected as handler parameters.
    internal static Ok<IReadOnlyList<TechnologyDto>> Handle(IBuiltInTechnologies builtIn, TechnologyStore store)
    {
        return TypedResults.Ok<IReadOnlyList<TechnologyDto>>([.. builtIn.All, .. store.GetAll()]);
    }
#endif
}
