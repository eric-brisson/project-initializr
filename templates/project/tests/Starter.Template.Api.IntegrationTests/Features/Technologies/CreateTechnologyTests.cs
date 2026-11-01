using System.Net;
using System.Net.Http.Json;
using Starter.Template.Api.Features.Technologies;
using Xunit;

namespace Starter.Template.Api.IntegrationTests.Features.Technologies;

public class CreateTechnologyTests(ApiFactory factory)
{
    [Fact]
    public async Task PostTechnology_ReturnsCreatedWithItsLocation()
    {
        var client = factory.CreateClient();
        var name = $"Created {Guid.NewGuid():N}";

        var response = await client.PostAsJsonAsync("/api/v1/technologies", new CreateTechnology.CreateTechnologyRequest(name), TestContext.Current.CancellationToken);
        var item = await response.Content.ReadFromJsonAsync<TechnologyDto>(TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.NotNull(item);
        Assert.Equal(name, item.Name);
        Assert.Equal($"/api/v1/technologies/{item.Id}", response.Headers.Location?.AbsolutePath);
    }

    [Fact]
    public async Task PostTechnology_WithMissingName_ReturnsBadRequest()
    {
        var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/v1/technologies", new { }, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
    }

    [Fact]
    public async Task PostTechnology_SameNameInAnotherCase_ReturnsConflict()
    {
        var client = factory.CreateClient();
        var name = $"Duplicate {Guid.NewGuid():N}";
        await client.PostAsJsonAsync("/api/v1/technologies", new CreateTechnology.CreateTechnologyRequest(name), TestContext.Current.CancellationToken);

        var response = await client.PostAsJsonAsync("/api/v1/technologies", new CreateTechnology.CreateTechnologyRequest(name.ToUpperInvariant()), TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
    }
}
