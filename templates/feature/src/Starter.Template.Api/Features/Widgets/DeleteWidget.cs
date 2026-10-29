using Microsoft.AspNetCore.Http.HttpResults;
#if (UseEfCore)
using Microsoft.EntityFrameworkCore;
using Starter.Template.Api.Common.Data;
#endif

namespace Starter.Template.Api.Features.Widgets;

public static class DeleteWidget
{
    public static void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapDelete("{id:int}", Handle)
            .WithName(nameof(DeleteWidget));
    }

#if (UseEfCore)
    internal static async Task<Results<NoContent, NotFound>> Handle(
        int id,
        AppDbContext db,
        CancellationToken cancellationToken)
    {
        // One DELETE statement, without loading the Widget first.
        var deleted = await db.Set<Widget>()
            .Where(item => item.Id == id)
            .ExecuteDeleteAsync(cancellationToken);

        return deleted == 0 ? TypedResults.NotFound() : TypedResults.NoContent();
    }
#else
    internal static Results<NoContent, NotFound> Handle(int id, WidgetStore store)
    {
        return store.Remove(id) ? TypedResults.NoContent() : TypedResults.NotFound();
    }
#endif
}
