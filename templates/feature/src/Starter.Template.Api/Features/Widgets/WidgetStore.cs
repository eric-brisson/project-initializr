namespace Starter.Template.Api.Features.Widgets;

// The Widgets, kept in memory until the app restarts (registered as a singleton).
// Add a database module to store them with EF Core instead.
public sealed class WidgetStore
{
    private readonly Lock _lock = new();
    private readonly Dictionary<int, Widget> _items = [];
    private int _lastId;

    public IReadOnlyList<Widget> GetAll()
    {
        lock (_lock)
        {
            return [.. _items.Values.OrderBy(item => item.Id)];
        }
    }

    public Widget? Find(int id)
    {
        lock (_lock)
        {
            return _items.GetValueOrDefault(id);
        }
    }

    // Gives the Widget its id.
    public Widget Add(Widget item)
    {
        lock (_lock)
        {
            item.Id = ++_lastId;
            _items[item.Id] = item;
            return item;
        }
    }

    // False when no Widget has this id.
    public bool Replace(Widget item)
    {
        lock (_lock)
        {
            if (!_items.ContainsKey(item.Id))
            {
                return false;
            }

            _items[item.Id] = item;
            return true;
        }
    }

    public bool Remove(int id)
    {
        lock (_lock)
        {
            return _items.Remove(id);
        }
    }
}
