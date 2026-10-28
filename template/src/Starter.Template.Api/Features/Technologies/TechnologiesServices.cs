namespace Starter.Template.Api.Features.Technologies;

public static class TechnologiesServices
{
    public static IServiceCollection AddTechnologiesServices(this IServiceCollection services)
    {
#if (UseDatabase || UseInMemory)
        // Scoped, like the DbContext it uses: one instance per request.
        services.AddScoped<ITechnologyCatalog, TechnologyCatalog>();
#else
        services.AddSingleton<ITechnologyCatalog, TechnologyCatalog>();
#endif

        return services;
    }
}
