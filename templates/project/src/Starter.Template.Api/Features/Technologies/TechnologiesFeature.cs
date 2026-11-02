namespace Starter.Template.Api.Features.Technologies;

// Le point d'entrée de la feature, appelé depuis Common/ServiceRegistration.cs et Common/EndpointRegistration.cs.
public static class TechnologiesFeature
{
    public static IServiceCollection AddTechnologiesServices(this IServiceCollection services)
    {
#if (UseEfCore)
        // Enregistrez ici les services de la feature. Les handlers reçoivent déjà AppDbContext.
#else
        // Singleton : les Technologies doivent survivre à chaque requête.
        services.AddSingleton<TechnologyStore>();
#endif

        return services;
    }

    // Une route par tranche, toutes sous le même groupe.
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
