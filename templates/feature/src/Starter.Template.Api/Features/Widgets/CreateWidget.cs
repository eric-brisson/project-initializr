using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http.HttpResults;
#if (UseEfCore)
using Starter.Template.Api.Common.Data;
#endif

namespace Starter.Template.Api.Features.Widgets;

public static class CreateWidget
{
    // TODO: add the other properties a client sends to create a Widget, with their validation attributes.
    public sealed record CreateWidgetRequest([property: Required, StringLength(100)] string Name);

    public static void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("", Handle)
            .WithName(nameof(CreateWidget));
    }

    // An invalid body gets a 400 problem details response before the handler runs (AddValidation in Program.cs).
#if (UseEfCore)
    internal static async Task<CreatedAtRoute<WidgetDto>> Handle(
        CreateWidgetRequest request,
        AppDbContext db,
        CancellationToken cancellationToken)
    {
        var item = new Widget { Name = request.Name };
        db.Set<Widget>().Add(item);
        await db.SaveChangesAsync(cancellationToken);

        return TypedResults.CreatedAtRoute(new WidgetDto(item.Id, item.Name), nameof(GetWidget), new { id = item.Id });
    }
#else
    internal static CreatedAtRoute<WidgetDto> Handle(CreateWidgetRequest request, WidgetStore store)
    {
        var item = store.Add(new Widget { Name = request.Name });

        return TypedResults.CreatedAtRoute(new WidgetDto(item.Id, item.Name), nameof(GetWidget), new { id = item.Id });
    }
#endif
}
