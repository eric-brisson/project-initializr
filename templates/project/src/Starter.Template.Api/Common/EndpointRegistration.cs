using Starter.Template.Api.Features.About;
using Starter.Template.Api.Features.Technologies;

namespace Starter.Template.Api.Common;

public static class EndpointRegistration
{
    public static IEndpointRouteBuilder MapEndpoints(this IEndpointRouteBuilder app)
    {
        // Le nom du groupe choisit le document OpenAPI (AddOpenApi() correspond à « v1 »). Un groupe v2 demande aussi AddOpenApi("v2").
        var v1 = app.MapGroup("/api/v1")
            .WithGroupName("v1");

        v1.MapAboutEndpoints();
        v1.MapTechnologiesEndpoints();

        return app;
    }
}
