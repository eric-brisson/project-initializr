using Starter.Template.Api.Features.Technologies;
using Xunit;

namespace Starter.Template.Api.UnitTests.Features.Technologies;

public class BuiltInTechnologiesTests
{
    [Fact]
    public void All_IncludesDotNetWithVersion()
    {
        Assert.Contains(BuiltInTechnologies.All, t => t.Name == ".NET" && t.Version.StartsWith("10."));
    }

    [Fact]
    public void All_EveryTechnologyHasAVersion()
    {
        Assert.All(BuiltInTechnologies.All, t => Assert.False(string.IsNullOrWhiteSpace(t.Version)));
    }

    [Fact]
    public void Contains_IgnoresCase()
    {
        Assert.True(BuiltInTechnologies.Contains(".net"));
    }
}
