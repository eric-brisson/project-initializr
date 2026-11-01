using System.Reflection;

namespace Starter.Template.Api.Features.About;

// The API and the main components it's built with, read from the loaded assemblies once.
internal static class TechStack
{
    public static GetAbout.AboutResponse About { get; } = new(
        typeof(Program).Assembly.GetName().Name ?? "unknown",
        VersionOf(typeof(Program)),
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
#if (UseEfCore)
            new("Entity Framework Core", VersionOf(typeof(Microsoft.EntityFrameworkCore.DbContext))),
#endif
        ]);

    private static string VersionOf(Type type)
    {
        var version = type.Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion
            ?? type.Assembly.GetName().Version?.ToString()
            ?? "unknown";

        // Drop the "+<commit hash>" suffix added to informational versions.
        return version.Split('+')[0];
    }
}
