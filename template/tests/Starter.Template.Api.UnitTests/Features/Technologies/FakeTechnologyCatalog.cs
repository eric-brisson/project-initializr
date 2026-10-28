using Starter.Template.Api.Features.Technologies;

namespace Starter.Template.Api.UnitTests.Features.Technologies;

// Stands in for the real catalog, so the handler tests don't depend on its storage.
internal sealed class FakeTechnologyCatalog(params ListTechnologies.TechnologyDto[] technologies) : ITechnologyCatalog
{
    private readonly List<ListTechnologies.TechnologyDto> _technologies = [.. technologies];

    public Task<IReadOnlyList<ListTechnologies.TechnologyDto>> GetAllAsync(CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyList<ListTechnologies.TechnologyDto>>(_technologies);

    public Task<bool> TryAddAsync(ListTechnologies.TechnologyDto technology, CancellationToken cancellationToken)
    {
        if (_technologies.Any(t => t.Name == technology.Name))
        {
            return Task.FromResult(false);
        }

        _technologies.Add(technology);
        return Task.FromResult(true);
    }
}
