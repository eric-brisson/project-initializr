using System.Net;
using System.Net.Http.Json;
using Starter.Template.Api.Features.Technologies;
using Xunit;

namespace Starter.Template.Api.IntegrationTests.Features.Technologies;

public class ListTechnologiesTests(ApiFactory factory)
{
    [Fact]
    public async Task GetTechnologies_ReturnsOkWithJsonList()
    {
        var client = factory.CreateClient();

        var response = await client.GetAsync("/api/v1/technologies", TestContext.Current.CancellationToken);
        var technologies = await response.Content.ReadFromJsonAsync<List<ListTechnologies.TechnologyDto>>(TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("application/json", response.Content.Headers.ContentType?.MediaType);
        Assert.NotNull(technologies);
        Assert.NotEmpty(technologies);
    }
}
