namespace Starter.Template.Api.Features.Technologies;

// A technology added through the API, stored in the Technologies table.
public sealed class Technology
{
    public int Id { get; set; }

    public required string Name { get; set; }

    public required string Version { get; set; }
}
