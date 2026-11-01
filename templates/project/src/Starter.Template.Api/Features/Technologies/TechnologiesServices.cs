namespace Starter.Template.Api.Features.Technologies;

public static class TechnologiesServices
{
    public static IServiceCollection AddTechnologiesServices(this IServiceCollection services)
    {
#if (UseEfCore)
        // Register the services of the feature here. The handlers already get AppDbContext.
#else
        // Singleton: the Technologies must outlive each request.
        services.AddSingleton<TechnologyStore>();
#endif

        return services;
    }
}
