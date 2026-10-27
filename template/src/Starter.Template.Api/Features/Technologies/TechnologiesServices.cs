namespace Starter.Template.Api.Features.Technologies;

public static class TechnologiesServices
{
    public static IServiceCollection AddTechnologiesServices(this IServiceCollection services)
    {
        services.AddSingleton<ITechnologyCatalog, TechnologyCatalog>();

        return services;
    }
}
