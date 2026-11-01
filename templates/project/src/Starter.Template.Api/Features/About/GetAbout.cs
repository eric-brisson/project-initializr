using Microsoft.AspNetCore.Http.HttpResults;

namespace Starter.Template.Api.Features.About;

public static class GetAbout
{
    public sealed record AboutResponse(string Name, string Version, IReadOnlyList<Component> Components);

    public sealed record Component(string Name, string Version);

    public static void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("", Handle)
            .WithName(nameof(GetAbout));
    }

    internal static Ok<AboutResponse> Handle() => TypedResults.Ok(TechStack.About);
}
