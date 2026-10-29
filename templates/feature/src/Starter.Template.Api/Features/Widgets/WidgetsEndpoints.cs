namespace Starter.Template.Api.Features.Widgets;

public static class WidgetsEndpoints
{
    public static IEndpointRouteBuilder MapWidgetsEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/widgets")
            .WithTags("Widgets");

        ListWidgets.MapEndpoint(group);
        GetWidget.MapEndpoint(group);
        CreateWidget.MapEndpoint(group);
        UpdateWidget.MapEndpoint(group);
        DeleteWidget.MapEndpoint(group);

        return app;
    }
}
