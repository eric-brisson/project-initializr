namespace Starter.Template.Api.Features.Technologies;

public static class TechnologiesEndpoints
{
    public static IEndpointRouteBuilder MapTechnologiesEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/technologies")
            .WithTags("Technologies");

        ListTechnologies.MapEndpoint(group);
        GetTechnology.MapEndpoint(group);
        CreateTechnology.MapEndpoint(group);
        UpdateTechnology.MapEndpoint(group);
        DeleteTechnology.MapEndpoint(group);

        return app;
    }
}
