namespace Starter.Template.Api.Features.Technologies;

public interface ITechnologyCatalog
{
    IReadOnlyList<ListTechnologies.TechnologyDto> GetAll();

    bool TryAdd(ListTechnologies.TechnologyDto technology);
}
