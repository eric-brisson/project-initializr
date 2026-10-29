using Microsoft.AspNetCore.Http.HttpResults;
#if (UseEfCore)
using Microsoft.EntityFrameworkCore;
using Starter.Template.Api.Common.Data;
#endif

namespace Starter.Template.Api.Features.Widgets;

public static class GetWidget
{
    public static void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("{id:int}", Handle)
            .WithName(nameof(GetWidget));
    }

#if (UseEfCore)
    internal static async Task<Results<Ok<WidgetDto>, NotFound>> Handle(
        int id,
        AppDbContext db,
        CancellationToken cancellationToken)
    {
        var item = await db.Set<Widget>()
            .Where(item => item.Id == id)
            .Select(item => new WidgetDto(item.Id, item.Name))
            .FirstOrDefaultAsync(cancellationToken);

        return item is null ? TypedResults.NotFound() : TypedResults.Ok(item);
    }
#else
    internal static Results<Ok<WidgetDto>, NotFound> Handle(int id, WidgetStore store)
    {
        return store.Find(id) is { } item
            ? TypedResults.Ok(new WidgetDto(item.Id, item.Name))
            : TypedResults.NotFound();
    }
#endif
}
