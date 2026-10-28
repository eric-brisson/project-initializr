using Starter.Template.Api.Features.Technologies;
using Xunit;

namespace Starter.Template.Api.UnitTests.Features.Technologies;

public class BuiltInTechnologiesTests
{
    private readonly BuiltInTechnologies _builtIn = new();

    [Fact]
    public void All_IncludesDotNetWithVersion()
    {
        Assert.Contains(_builtIn.All, t => t.Name == ".NET" && t.Version.StartsWith("10."));
    }

    [Fact]
    public void All_EveryTechnologyHasAVersion()
    {
        Assert.All(_builtIn.All, t => Assert.False(string.IsNullOrWhiteSpace(t.Version)));
    }

    [Fact]
    public void Contains_IgnoresCase()
    {
        Assert.True(_builtIn.Contains(".net"));
    }
}
