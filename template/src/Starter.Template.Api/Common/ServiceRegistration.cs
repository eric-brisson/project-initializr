using Starter.Template.Api.Features.Technologies;

namespace Starter.Template.Api.Common;

public static class ServiceRegistration
{
    public static IServiceCollection AddFeatures(this IServiceCollection services)
    {
        services.AddTechnologiesServices();

        return services;
    }
}
