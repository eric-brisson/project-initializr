namespace Starter.Template.Api.Features.About;

// Le point d'entrée de la feature, appelé depuis Common/EndpointRegistration.cs. Elle n'a aucun service à enregistrer.
public static class AboutFeature
{
    public static IEndpointRouteBuilder MapAboutEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/about")
            .WithTags("About");

        GetAbout.MapEndpoint(group);

        return app;
    }
}
