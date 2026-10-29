namespace Starter.Template.Api.Features.Widgets;

public static class WidgetsServices
{
    public static IServiceCollection AddWidgetsServices(this IServiceCollection services)
    {
#if (UseEfCore)
        // TODO: register the services of the feature. The handlers already get AppDbContext.
#else
        // Singleton: the Widgets must outlive each request.
        services.AddSingleton<WidgetStore>();
#endif

        return services;
    }
}
