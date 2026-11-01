namespace Starter.Template.Api.Features.About;

public static class AboutEndpoints
{
    public static IEndpointRouteBuilder MapAboutEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/about")
            .WithTags("About");

        GetAbout.MapEndpoint(group);

        return app;
    }
}
