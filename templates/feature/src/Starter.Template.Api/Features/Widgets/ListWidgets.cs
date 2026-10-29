using Microsoft.AspNetCore.Http.HttpResults;
#if (UseEfCore)
using Microsoft.EntityFrameworkCore;
using Starter.Template.Api.Common.Data;
#endif

namespace Starter.Template.Api.Features.Widgets;

public static class ListWidgets
{
    public static void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("", Handle)
            .WithName(nameof(ListWidgets));
    }

#if (UseEfCore)
    internal static async Task<Ok<List<WidgetDto>>> Handle(AppDbContext db, CancellationToken cancellationToken)
    {
        // Projected straight to the DTO: EF Core reads only these columns and doesn't track anything.
        var items = await db.Set<Widget>()
            .OrderBy(item => item.Id)
            .Select(item => new WidgetDto(item.Id, item.Name))
            .ToListAsync(cancellationToken);

        return TypedResults.Ok(items);
    }
#else
    internal static Ok<List<WidgetDto>> Handle(WidgetStore store)
    {
        return TypedResults.Ok(store.GetAll().Select(item => new WidgetDto(item.Id, item.Name)).ToList());
    }
#endif
}
