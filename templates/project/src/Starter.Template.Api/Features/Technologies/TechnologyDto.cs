using System.Linq.Expressions;

namespace Starter.Template.Api.Features.Technologies;

// What the API returns for a Technology.
//#if (IsFeature)
// TODO: add the properties the API returns, then map them in Projection.
//#endif
public sealed record TechnologyDto(int Id, string Name)
{
    // The one mapping from the entity to the DTO. EF Core translates it to SQL, so a query reads only these columns
    // and tracks nothing. From runs the same mapping on an entity already in memory.
    public static readonly Expression<Func<Technology, TechnologyDto>> Projection =
        item => new TechnologyDto(item.Id, item.Name);

    private static readonly Func<Technology, TechnologyDto> Map = Projection.Compile();

    public static TechnologyDto From(Technology item) => Map(item);
}
