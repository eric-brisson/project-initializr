using Starter.Template.Api.Features.Technologies;
using Xunit;

namespace Starter.Template.Api.UnitTests.Features.Technologies;

// The store of a project without a data module. With EF Core, the integration tests cover the database.
public class TechnologyStoreTests
{
    private readonly TechnologyStore _store = new();

    [Fact]
    public void TryAdd_NewName_GivesAnId()
    {
        var item = new Technology { Name = "First" };

        var added = _store.TryAdd(item);

        Assert.True(added);
        Assert.Equal(1, item.Id);
        Assert.Same(item, _store.Find(item.Id));
    }

    [Fact]
    public void TryAdd_SameNameInAnotherCase_IsRejected()
    {
        _store.TryAdd(new Technology { Name = "Taken" });

        var added = _store.TryAdd(new Technology { Name = "TAKEN" });

        Assert.False(added);
        Assert.Single(_store.GetAll());
    }

    [Fact]
    public void TryReplace_UnknownId_IsNotFound()
    {
        var result = _store.TryReplace(new Technology { Id = 42, Name = "Unknown" });

        Assert.Equal(TechnologyStore.ReplaceResult.NotFound, result);
    }

    [Fact]
    public void TryReplace_NameOfAnotherOne_IsRejected()
    {
        _store.TryAdd(new Technology { Name = "Taken" });
        var item = new Technology { Name = "Free" };
        _store.TryAdd(item);

        var result = _store.TryReplace(new Technology { Id = item.Id, Name = "taken" });

        Assert.Equal(TechnologyStore.ReplaceResult.NameTaken, result);
    }

    [Fact]
    public void TryReplace_SameNameInAnotherCase_IsReplaced()
    {
        var item = new Technology { Name = "Renamed" };
        _store.TryAdd(item);

        var result = _store.TryReplace(new Technology { Id = item.Id, Name = "RENAMED" });

        Assert.Equal(TechnologyStore.ReplaceResult.Replaced, result);
        Assert.Equal("RENAMED", _store.Find(item.Id)?.Name);
    }

    [Fact]
    public void Remove_DeletesIt()
    {
        var item = new Technology { Name = "Removed" };
        _store.TryAdd(item);

        Assert.True(_store.Remove(item.Id));
        Assert.Null(_store.Find(item.Id));
        Assert.False(_store.Remove(item.Id));
    }
}
