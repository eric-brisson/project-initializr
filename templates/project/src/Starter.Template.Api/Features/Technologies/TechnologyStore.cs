namespace Starter.Template.Api.Features.Technologies;

// The Technologies, kept in memory until the app restarts (registered as a singleton), for a project without
// a data module. Names are unique in any case, like the unique index of the database version.
public sealed class TechnologyStore
{
    private readonly Lock _lock = new();
    private readonly Dictionary<int, Technology> _items = [];
    private int _lastId;

    public enum ReplaceResult
    {
        Replaced,
        NotFound,
        NameTaken,
    }

    public IReadOnlyList<Technology> GetAll()
    {
        lock (_lock)
        {
            return [.. _items.Values.OrderBy(item => item.Id)];
        }
    }

    public Technology? Find(int id)
    {
        lock (_lock)
        {
            return _items.GetValueOrDefault(id);
        }
    }

    // Gives the Technology its id. False when another one already has the name.
    public bool TryAdd(Technology item)
    {
        lock (_lock)
        {
            if (IsNameTaken(item.Name, item.Id))
            {
                return false;
            }

            item.Id = ++_lastId;
            _items[item.Id] = item;
            return true;
        }
    }

    public ReplaceResult TryReplace(Technology item)
    {
        lock (_lock)
        {
            if (!_items.ContainsKey(item.Id))
            {
                return ReplaceResult.NotFound;
            }

            if (IsNameTaken(item.Name, item.Id))
            {
                return ReplaceResult.NameTaken;
            }

            _items[item.Id] = item;
            return ReplaceResult.Replaced;
        }
    }

    public bool Remove(int id)
    {
        lock (_lock)
        {
            return _items.Remove(id);
        }
    }

    private bool IsNameTaken(string name, int exceptId) =>
        _items.Values.Any(other => other.Id != exceptId && string.Equals(other.Name, name, StringComparison.OrdinalIgnoreCase));
}
