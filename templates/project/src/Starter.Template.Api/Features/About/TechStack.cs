using System.Reflection;

namespace Starter.Template.Api.Features.About;

// L'API et ses principaux composants, lus une seule fois dans les assemblies chargées.
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
#if (UseTelemetry)
            new("OpenTelemetry", VersionOf(typeof(OpenTelemetry.Sdk))),
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

        // Retire le suffixe « +<hash du commit> » ajouté aux versions informatives.
        return version.Split('+')[0];
    }
}
