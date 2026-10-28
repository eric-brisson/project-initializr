using System.Reflection;

namespace Starter.Template.Api.Features.Technologies;

// The technologies the project is built with, read from the loaded assemblies.
public static class BuiltInTechnologies
{
    public static IReadOnlyList<ListTechnologies.TechnologyDto> All { get; } =
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
#if (UseDatabase || UseInMemory)
        new("Entity Framework Core", VersionOf(typeof(Microsoft.EntityFrameworkCore.DbContext))),
#endif
    ];

    public static bool Contains(string name) =>
        All.Any(technology => string.Equals(technology.Name, name, StringComparison.OrdinalIgnoreCase));

    private static string VersionOf(Type type)
    {
        var version = type.Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion
            ?? type.Assembly.GetName().Version?.ToString()
            ?? "unknown";

        // Drop the "+<commit hash>" suffix added to informational versions.
        return version.Split('+')[0];
    }
}
