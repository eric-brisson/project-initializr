// Only without a database module: the EF Core path is covered by the integration tests.
#if (!UseDatabase && !UseInMemory)
using Starter.Template.Api.Features.Technologies;
using Xunit;

namespace Starter.Template.Api.UnitTests.Features.Technologies;

public class TechnologyStoreTests
{
    private readonly TechnologyStore _store = new();

    [Fact]
    public void TryAdd_NewTechnology_AddsIt()
    {
        var added = _store.TryAdd(new("New Tech", "1.0.0"));

        Assert.True(added);
        Assert.Equal(new ListTechnologies.TechnologyDto("New Tech", "1.0.0"), Assert.Single(_store.GetAll()));
    }

    [Fact]
    public void TryAdd_SameNameInAnotherCase_IsRejected()
    {
        _store.TryAdd(new("New Tech", "1.0.0"));

        var added = _store.TryAdd(new("NEW TECH", "2.0.0"));

        Assert.False(added);
    }
}
#endif
