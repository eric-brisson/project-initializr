namespace Starter.Template.Api.Features.Technologies;

public interface IBuiltInTechnologies
{
    IReadOnlyList<ListTechnologies.TechnologyDto> All { get; }

    bool Contains(string name);
}
