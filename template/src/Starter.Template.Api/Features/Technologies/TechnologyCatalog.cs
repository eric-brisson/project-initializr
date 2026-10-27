using System.Reflection;

namespace Starter.Template.Api.Features.Technologies;

// Registered as a singleton, so the added technologies live in memory until the app restarts.
public sealed class TechnologyCatalog : ITechnologyCatalog
{
    private readonly Lock _lock = new();
    private readonly List<ListTechnologies.TechnologyDto> _added = [];

    public IReadOnlyList<ListTechnologies.TechnologyDto> GetAll()
    {
        lock (_lock)
        {
            return [.. BuiltIn(), .. _added];
        }
    }

    public bool TryAdd(ListTechnologies.TechnologyDto technology)
    {
        lock (_lock)
        {
            if (BuiltIn().Concat(_added).Any(t => string.Equals(t.Name, technology.Name, StringComparison.OrdinalIgnoreCase)))
            {
                return false;
            }

            _added.Add(technology);
            return true;
        }
    }

    private static List<ListTechnologies.TechnologyDto> BuiltIn() =>
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

    private static string VersionOf(Type type)
    {
        var version = type.Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion
            ?? type.Assembly.GetName().Version?.ToString()
            ?? "unknown";

        // Drop the "+<commit hash>" suffix added to informational versions.
        return version.Split('+')[0];
    }
}
