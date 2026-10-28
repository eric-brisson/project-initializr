namespace Starter.Template.Api.Features.Technologies;

public static class TechnologiesServices
{
    public static IServiceCollection AddTechnologiesServices(this IServiceCollection services)
    {
        // Singleton: the built-in list is read from the assemblies once.
        services.AddSingleton<IBuiltInTechnologies, BuiltInTechnologies>();
#if (!UseDatabase && !UseInMemory)

        // Singleton too: the added technologies must outlive each request.
        services.AddSingleton<TechnologyStore>();
#endif

        return services;
    }
}
