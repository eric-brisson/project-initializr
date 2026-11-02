namespace Starter.Template.Api.Features.Technologies;

// Ce que l'API renvoie pour chaque Technology.
//#if (IsFeature)
// TODO : ajoutez les propriétés que l'API renvoie, puis remplissez-les dans From.
//#endif
public sealed record TechnologyDto(int Id, string Name)
{
    // Le seul endroit qui transforme l'entité en ce que l'API renvoie.
    public static TechnologyDto From(Technology item) => new(item.Id, item.Name);
}
