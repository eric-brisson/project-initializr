namespace Starter.Template.Api.Features.Technologies;

public interface ITechnologyCatalog
{
    Task<IReadOnlyList<ListTechnologies.TechnologyDto>> GetAllAsync(CancellationToken cancellationToken);

    Task<bool> TryAddAsync(ListTechnologies.TechnologyDto technology, CancellationToken cancellationToken);
}
