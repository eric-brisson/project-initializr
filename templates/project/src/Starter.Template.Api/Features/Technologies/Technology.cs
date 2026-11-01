namespace Starter.Template.Api.Features.Technologies;

// The entity, as stored. Only the slices of this folder use it: the API returns TechnologyDto.
public sealed class Technology
{
    public int Id { get; set; }

    public required string Name { get; set; }
    //#if (IsFeature)

    // TODO: add the other properties of a Technology.
    //#endif
}
