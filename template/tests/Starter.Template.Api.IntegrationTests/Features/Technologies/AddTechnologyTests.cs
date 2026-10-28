using System.Net;
using System.Net.Http.Json;
using Starter.Template.Api.Features.Technologies;
using Xunit;

namespace Starter.Template.Api.IntegrationTests.Features.Technologies;

public class AddTechnologyTests(ApiFactory factory)
{
    [Fact]
    public async Task PostTechnology_ReturnsCreatedAndAddsItToTheList()
    {
        var client = factory.CreateClient();
        var request = new AddTechnology.AddTechnologyRequest("Integration Test Tech", "1.0.0");

        var response = await client.PostAsJsonAsync("/api/v1/technologies", request, TestContext.Current.CancellationToken);
        var technologies = await client.GetFromJsonAsync<List<ListTechnologies.TechnologyDto>>("/api/v1/technologies", TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.Equal("/api/v1/technologies", response.Headers.Location?.AbsolutePath);
        Assert.NotNull(technologies);
        Assert.Contains(new ListTechnologies.TechnologyDto("Integration Test Tech", "1.0.0"), technologies);
    }

    [Fact]
    public async Task PostTechnology_WithExistingName_ReturnsConflict()
    {
        var client = factory.CreateClient();
        var request = new AddTechnology.AddTechnologyRequest(".NET", "1.0.0");

        var response = await client.PostAsJsonAsync("/api/v1/technologies", request, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
    }

    [Fact]
    public async Task PostTechnology_SameNameInAnotherCase_ReturnsConflict()
    {
        var client = factory.CreateClient();
        await client.PostAsJsonAsync("/api/v1/technologies", new AddTechnology.AddTechnologyRequest("Case Test Tech", "1.0.0"), TestContext.Current.CancellationToken);

        var response = await client.PostAsJsonAsync("/api/v1/technologies", new AddTechnology.AddTechnologyRequest("CASE TEST TECH", "2.0.0"), TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task PostTechnology_WithMissingName_ReturnsBadRequest()
    {
        var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/v1/technologies", new { version = "1.0.0" }, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
    }
}
