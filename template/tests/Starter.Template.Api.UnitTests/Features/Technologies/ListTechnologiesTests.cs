using Starter.Template.Api.Features.Technologies;
using Xunit;

namespace Starter.Template.Api.UnitTests.Features.Technologies;

public class ListTechnologiesTests
{
    [Fact]
    public async Task Handle_ReturnsTechnologiesFromCatalog()
    {
        var catalog = new FakeTechnologyCatalog(new ListTechnologies.TechnologyDto("Fake", "1.2.3"));

        var result = await ListTechnologies.Handle(catalog, TestContext.Current.CancellationToken);

        Assert.NotNull(result.Value);
        var technology = Assert.Single(result.Value);
        Assert.Equal("Fake", technology.Name);
        Assert.Equal("1.2.3", technology.Version);
    }
}
