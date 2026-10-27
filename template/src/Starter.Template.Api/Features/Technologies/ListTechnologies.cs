using Microsoft.AspNetCore.Http.HttpResults;

namespace Starter.Template.Api.Features.Technologies;

public static class ListTechnologies
{
    public sealed record TechnologyDto(string Name, string Version);

    public static void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("", Handle)
            .WithName(nameof(ListTechnologies));
    }

    // Services registered in the DI container are injected as handler parameters.
    internal static Ok<IReadOnlyList<TechnologyDto>> Handle(ITechnologyCatalog catalog)
    {
        return TypedResults.Ok(catalog.GetAll());
    }
}
