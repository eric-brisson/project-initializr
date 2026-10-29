namespace Starter.Template.Api.Features.Technologies;

// The technologies added through the API. Registered as a singleton, so they live in memory until the app restarts.
// Add a database module (inmemory or database) to store them with EF Core instead.
public sealed class TechnologyStore
{
    private readonly Lock _lock = new();
    private readonly List<ListTechnologies.TechnologyDto> _technologies = [];

    public IReadOnlyList<ListTechnologies.TechnologyDto> GetAll()
    {
        lock (_lock)
        {
            return [.. _technologies];
        }
    }

    // False when a technology with the same name (in any case) is already stored.
    public bool TryAdd(ListTechnologies.TechnologyDto technology)
    {
        lock (_lock)
        {
            if (_technologies.Any(t => string.Equals(t.Name, technology.Name, StringComparison.OrdinalIgnoreCase)))
            {
                return false;
            }

            _technologies.Add(technology);
            return true;
        }
    }
}
