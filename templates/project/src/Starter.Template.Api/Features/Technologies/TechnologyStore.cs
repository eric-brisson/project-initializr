namespace Starter.Template.Api.Features.Technologies;

// Garde les Technologies en mémoire jusqu'au redémarrage de l'application (enregistré en singleton), pour un projet
// sans module de données. Les noms sont uniques sans tenir compte de la casse, comme l'index unique de la version base de données.
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

    // Attribue un id à l'élément. False quand un autre élément porte déjà ce nom.
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
