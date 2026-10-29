using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http.HttpResults;
#if (UseEfCore)
using Starter.Template.Api.Common.Data;
#endif

namespace Starter.Template.Api.Features.Widgets;

public static class UpdateWidget
{
    // TODO: add the other properties a client can change, with their validation attributes.
    public sealed record UpdateWidgetRequest([property: Required, StringLength(100)] string Name);

    public static void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPut("{id:int}", Handle)
            .WithName(nameof(UpdateWidget));
    }

#if (UseEfCore)
    internal static async Task<Results<NoContent, NotFound>> Handle(
        int id,
        UpdateWidgetRequest request,
        AppDbContext db,
        CancellationToken cancellationToken)
    {
        // Tracked: EF Core saves only the properties that changed.
        var item = await db.Set<Widget>().FindAsync([id], cancellationToken);
        if (item is null)
        {
            return TypedResults.NotFound();
        }

        item.Name = request.Name;
        await db.SaveChangesAsync(cancellationToken);

        return TypedResults.NoContent();
    }
#else
    internal static Results<NoContent, NotFound> Handle(int id, UpdateWidgetRequest request, WidgetStore store)
    {
        return store.Replace(new Widget { Id = id, Name = request.Name })
            ? TypedResults.NoContent()
            : TypedResults.NotFound();
    }
#endif
}
