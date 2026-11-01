using Starter.Template.Api.Features.About;
using Starter.Template.Api.Features.Technologies;

namespace Starter.Template.Api.Common;

public static class EndpointRegistration
{
    public static IEndpointRouteBuilder MapEndpoints(this IEndpointRouteBuilder app)
    {
        // The group name picks the OpenAPI document (AddOpenApi() is "v1"). A v2 group needs AddOpenApi("v2") too.
        var v1 = app.MapGroup("/api/v1")
            .WithGroupName("v1");

        v1.MapAboutEndpoints();
        v1.MapTechnologiesEndpoints();

        return app;
    }
}
