using Starter.Template.Api.Features.Technologies;
using Xunit;

namespace Starter.Template.Api.UnitTests.Features.Technologies;

public class ListTechnologiesTests
{
    [Fact]
    public async Task Handle_IncludesDotNetWithVersion()
    {
        var result = await ListTechnologies.Handle();

        Assert.NotNull(result.Value);
        Assert.Contains(result.Value, t => t.Name == ".NET" && t.Version.StartsWith("10."));
    }

    [Fact]
    public async Task Handle_EveryTechnologyHasAVersion()
    {
        var result = await ListTechnologies.Handle();

        Assert.NotNull(result.Value);
        Assert.All(result.Value, t => Assert.False(string.IsNullOrWhiteSpace(t.Version)));
    }
}
