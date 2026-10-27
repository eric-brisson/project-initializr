namespace Starter.Template.Api.Features.Technologies;

public static class TechnologiesEndpoints
{
    public static IEndpointRouteBuilder MapTechnologiesEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/technologies")
            .WithTags("Technologies");

        ListTechnologies.MapEndpoint(group);
        AddTechnology.MapEndpoint(group);

        return app;
    }
}
