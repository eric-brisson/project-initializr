using Starter.Template.Api.Features.Technologies;
using Xunit;

namespace Starter.Template.Api.UnitTests.Features.Technologies;

public class ListTechnologiesTests
{
    [Fact]
    public void Handle_ReturnsTechnologiesFromCatalog()
    {
        var catalog = new FakeTechnologyCatalog([new("Fake", "1.2.3")]);

        var result = ListTechnologies.Handle(catalog);

        Assert.NotNull(result.Value);
        var technology = Assert.Single(result.Value);
        Assert.Equal("Fake", technology.Name);
        Assert.Equal("1.2.3", technology.Version);
    }

    private sealed class FakeTechnologyCatalog(IReadOnlyList<ListTechnologies.TechnologyDto> technologies)
        : ITechnologyCatalog
    {
        public IReadOnlyList<ListTechnologies.TechnologyDto> GetAll() => technologies;

        public bool TryAdd(ListTechnologies.TechnologyDto technology) => throw new NotSupportedException();
    }
}
