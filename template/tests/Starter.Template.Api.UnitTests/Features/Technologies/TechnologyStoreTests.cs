// Only without a database module: the EF Core catalog is covered by the integration tests.
#if (!UseDatabase && !UseInMemory)
using Starter.Template.Api.Features.Technologies;
using Xunit;

namespace Starter.Template.Api.UnitTests.Features.Technologies;

public class TechnologyCatalogTests
{
    private readonly TechnologyCatalog _catalog = new();

    [Fact]
    public async Task TryAddAsync_NewTechnology_AddsItAfterTheBuiltInOnes()
    {
        var added = await _catalog.TryAddAsync(new("New Tech", "1.0.0"), TestContext.Current.CancellationToken);

        var technologies = await _catalog.GetAllAsync(TestContext.Current.CancellationToken);
        Assert.True(added);
        Assert.Equal(new ListTechnologies.TechnologyDto("New Tech", "1.0.0"), technologies[^1]);
    }

    [Fact]
    public async Task TryAddAsync_SameNameInAnotherCase_IsRejected()
    {
        await _catalog.TryAddAsync(new("New Tech", "1.0.0"), TestContext.Current.CancellationToken);

        var added = await _catalog.TryAddAsync(new("NEW TECH", "2.0.0"), TestContext.Current.CancellationToken);

        Assert.False(added);
    }
}
#endif
