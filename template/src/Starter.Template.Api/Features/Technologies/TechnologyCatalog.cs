#if (UseDatabase || UseInMemory)
using Microsoft.EntityFrameworkCore;
using Starter.Template.Api.Common.Data;

namespace Starter.Template.Api.Features.Technologies;

// The built-in technologies, followed by the ones added through the API and stored in the database.
public sealed class TechnologyCatalog(AppDbContext db) : ITechnologyCatalog
{
    public async Task<IReadOnlyList<ListTechnologies.TechnologyDto>> GetAllAsync(CancellationToken cancellationToken)
    {
        var added = await db.Technologies
            .AsNoTracking()
            .OrderBy(technology => technology.Id)
            .Select(technology => new ListTechnologies.TechnologyDto(technology.Name, technology.Version))
            .ToListAsync(cancellationToken);

        return [.. BuiltInTechnologies.All, .. added];
    }

    public async Task<bool> TryAddAsync(ListTechnologies.TechnologyDto technology, CancellationToken cancellationToken)
    {
        // Upper-cased on both sides, since SQL Server compares names case-insensitively by default and SQLite doesn't.
        // The unique index on Name still rejects a duplicate inserted by a concurrent request.
        var name = technology.Name.ToUpperInvariant();
        if (BuiltInTechnologies.Contains(technology.Name)
            || await db.Technologies.AnyAsync(existing => existing.Name.ToUpper() == name, cancellationToken))
        {
            return false;
        }

        db.Technologies.Add(new Technology { Name = technology.Name, Version = technology.Version });
        await db.SaveChangesAsync(cancellationToken);
        return true;
    }
}
#else
namespace Starter.Template.Api.Features.Technologies;

// Registered as a singleton, so the added technologies live in memory until the app restarts.
public sealed class TechnologyCatalog : ITechnologyCatalog
{
    private readonly Lock _lock = new();
    private readonly List<ListTechnologies.TechnologyDto> _added = [];

    public Task<IReadOnlyList<ListTechnologies.TechnologyDto>> GetAllAsync(CancellationToken cancellationToken)
    {
        lock (_lock)
        {
            return Task.FromResult<IReadOnlyList<ListTechnologies.TechnologyDto>>([.. BuiltInTechnologies.All, .. _added]);
        }
    }

    public Task<bool> TryAddAsync(ListTechnologies.TechnologyDto technology, CancellationToken cancellationToken)
    {
        lock (_lock)
        {
            if (BuiltInTechnologies.Contains(technology.Name)
                || _added.Any(t => string.Equals(t.Name, technology.Name, StringComparison.OrdinalIgnoreCase)))
            {
                return Task.FromResult(false);
            }

            _added.Add(technology);
            return Task.FromResult(true);
        }
    }
}
#endif
