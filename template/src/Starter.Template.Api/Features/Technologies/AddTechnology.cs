using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http.HttpResults;

namespace Starter.Template.Api.Features.Technologies;

public static class AddTechnology
{
    public sealed record AddTechnologyRequest(
        [property: Required, StringLength(100)] string Name,
        [property: Required, StringLength(50)] string Version);

    public static void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("", Handle)
            .WithName(nameof(AddTechnology));
    }

    // The request is bound from the JSON body and validated before the handler runs
    // (see AddValidation in Program.cs): an invalid body gets a 400 problem details response.
    internal static async Task<Results<CreatedAtRoute<ListTechnologies.TechnologyDto>, ProblemHttpResult>> Handle(
        AddTechnologyRequest request,
        ITechnologyCatalog catalog,
        CancellationToken cancellationToken)
    {
        var technology = new ListTechnologies.TechnologyDto(request.Name, request.Version);

        if (!await catalog.TryAddAsync(technology, cancellationToken))
        {
            return TypedResults.Problem(
                statusCode: StatusCodes.Status409Conflict,
                title: "Technology already exists.",
                detail: $"A technology named '{request.Name}' is already in the catalog.");
        }

        return TypedResults.CreatedAtRoute(technology, nameof(ListTechnologies));
    }
}
