using Starter.Template.Api.Features.Technologies;

namespace Starter.Template.Api.Common;

public static class EndpointRegistration
{
    public static IEndpointRouteBuilder MapEndpoints(this IEndpointRouteBuilder app)
    {
        var v1 = app.MapGroup("/api/v1")
            .WithGroupName("v1");

        v1.MapTechnologiesEndpoints();

        return app;
    }
}
