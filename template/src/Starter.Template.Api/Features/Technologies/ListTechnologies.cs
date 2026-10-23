using System.Reflection;
using Microsoft.AspNetCore.Http.HttpResults;

namespace Starter.Template.Api.Features.Technologies;

public static class ListTechnologies
{
    public sealed record TechnologyDto(string Name, string Version);

    public static void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("", Handle)
            .WithName(nameof(ListTechnologies));
    }

    internal static Task<Ok<List<TechnologyDto>>> Handle()
    {
        List<TechnologyDto> technologies =
        [
            new(".NET", Environment.Version.ToString()),
            new("ASP.NET Core", VersionOf(typeof(WebApplication))),
            new("OpenAPI", VersionOf(typeof(Microsoft.AspNetCore.OpenApi.OpenApiOptions))),
#if (UseScalar)
            new("Scalar", VersionOf(typeof(Scalar.AspNetCore.ScalarOptions))),
#endif
#if (UseSwaggerUi)
            new("Swagger UI", VersionOf(typeof(Swashbuckle.AspNetCore.SwaggerUI.SwaggerUIOptions))),
#endif
        ];

        return Task.FromResult(TypedResults.Ok(technologies));
    }

    private static string VersionOf(Type type)
    {
        var version = type.Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion
            ?? type.Assembly.GetName().Version?.ToString()
            ?? "unknown";

        // Drop the "+<commit hash>" suffix added to informational versions.
        return version.Split('+')[0];
    }
}
