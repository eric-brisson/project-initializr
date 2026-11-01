using Starter.Template.Api.Features.About;
using Xunit;

namespace Starter.Template.Api.UnitTests.Features.About;

public class TechStackTests
{
    [Fact]
    public void About_NamesTheApi()
    {
        Assert.Equal("Starter.Template.Api", TechStack.About.Name);
    }

    [Fact]
    public void About_IncludesDotNetWithVersion()
    {
        Assert.Contains(TechStack.About.Components, component => component.Name == ".NET" && component.Version.StartsWith("10.", StringComparison.Ordinal));
    }

    [Fact]
    public void About_EveryComponentHasAVersion()
    {
        Assert.All(TechStack.About.Components, component => Assert.False(string.IsNullOrWhiteSpace(component.Version)));
    }
}
