using System.Net;
using System.Net.Http.Json;
using Starter.Template.Api.Features.Technologies;
using Xunit;

namespace Starter.Template.Api.IntegrationTests.Features.Technologies;

public class UpdateTechnologyTests(ApiFactory factory)
{
    [Fact]
    public async Task PutTechnology_UpdatesIt()
    {
        var client = factory.CreateClient();
        var created = await client.PostAsJsonAsync("/api/v1/technologies", new CreateTechnology.CreateTechnologyRequest($"Before {Guid.NewGuid():N}"), TestContext.Current.CancellationToken);
        var newName = $"After {Guid.NewGuid():N}";

        var response = await client.PutAsJsonAsync(created.Headers.Location, new UpdateTechnology.UpdateTechnologyRequest(newName), TestContext.Current.CancellationToken);
        var item = await client.GetFromJsonAsync<TechnologyDto>(created.Headers.Location, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        Assert.Equal(newName, item?.Name);
    }

    [Fact]
    public async Task PutTechnology_UnknownId_ReturnsNotFound()
    {
        var client = factory.CreateClient();

        var response = await client.PutAsJsonAsync($"/api/v1/technologies/{int.MaxValue}", new UpdateTechnology.UpdateTechnologyRequest("Unknown"), TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task PutTechnology_NameOfAnotherOne_ReturnsConflict()
    {
        var client = factory.CreateClient();
        var taken = $"Taken {Guid.NewGuid():N}";
        await client.PostAsJsonAsync("/api/v1/technologies", new CreateTechnology.CreateTechnologyRequest(taken), TestContext.Current.CancellationToken);
        var created = await client.PostAsJsonAsync("/api/v1/technologies", new CreateTechnology.CreateTechnologyRequest($"Free {Guid.NewGuid():N}"), TestContext.Current.CancellationToken);

        var response = await client.PutAsJsonAsync(created.Headers.Location, new UpdateTechnology.UpdateTechnologyRequest(taken), TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
    }

    [Fact]
    public async Task PutTechnology_WithMissingName_ReturnsBadRequest()
    {
        var client = factory.CreateClient();

        var response = await client.PutAsJsonAsync("/api/v1/technologies/1", new { }, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
}
