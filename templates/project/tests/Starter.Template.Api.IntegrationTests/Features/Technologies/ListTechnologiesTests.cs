using System.Net;
using System.Net.Http.Json;
using Starter.Template.Api.Features.Technologies;
using Xunit;

namespace Starter.Template.Api.IntegrationTests.Features.Technologies;

public class ListTechnologiesTests(ApiFactory factory)
{
    [Fact]
    public async Task GetTechnologies_ReturnsTheCreatedTechnology()
    {
        var client = factory.CreateClient();
        var created = await client.PostAsJsonAsync("/api/v1/technologies", new CreateTechnology.CreateTechnologyRequest($"Listed {Guid.NewGuid():N}"), TestContext.Current.CancellationToken);
        var item = await created.Content.ReadFromJsonAsync<TechnologyDto>(TestContext.Current.CancellationToken);

        var response = await client.GetAsync("/api/v1/technologies", TestContext.Current.CancellationToken);
        var items = await response.Content.ReadFromJsonAsync<List<TechnologyDto>>(TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("application/json", response.Content.Headers.ContentType?.MediaType);
        Assert.NotNull(items);
        Assert.Contains(item, items);
    }
}
