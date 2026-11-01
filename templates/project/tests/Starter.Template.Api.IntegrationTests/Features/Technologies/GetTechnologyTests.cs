using System.Net;
using System.Net.Http.Json;
using Starter.Template.Api.Features.Technologies;
using Xunit;

namespace Starter.Template.Api.IntegrationTests.Features.Technologies;

public class GetTechnologyTests(ApiFactory factory)
{
    [Fact]
    public async Task GetTechnology_ReturnsIt()
    {
        var client = factory.CreateClient();
        var name = $"Read {Guid.NewGuid():N}";
        var created = await client.PostAsJsonAsync("/api/v1/technologies", new CreateTechnology.CreateTechnologyRequest(name), TestContext.Current.CancellationToken);

        var item = await client.GetFromJsonAsync<TechnologyDto>(created.Headers.Location, TestContext.Current.CancellationToken);

        Assert.NotNull(item);
        Assert.Equal(name, item.Name);
    }

    [Fact]
    public async Task GetTechnology_UnknownId_ReturnsNotFoundProblem()
    {
        var client = factory.CreateClient();

        var response = await client.GetAsync($"/api/v1/technologies/{int.MaxValue}", TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
    }
}
