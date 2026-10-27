using Starter.Template.Api.Features.Technologies;
using Xunit;

namespace Starter.Template.Api.UnitTests.Features.Technologies;

public class TechnologyCatalogTests
{
    private readonly TechnologyCatalog _catalog = new();

    [Fact]
    public void GetAll_IncludesDotNetWithVersion()
    {
        var technologies = _catalog.GetAll();

        Assert.Contains(technologies, t => t.Name == ".NET" && t.Version.StartsWith("10."));
    }

    [Fact]
    public void GetAll_EveryTechnologyHasAVersion()
    {
        var technologies = _catalog.GetAll();

        Assert.All(technologies, t => Assert.False(string.IsNullOrWhiteSpace(t.Version)));
    }
}
