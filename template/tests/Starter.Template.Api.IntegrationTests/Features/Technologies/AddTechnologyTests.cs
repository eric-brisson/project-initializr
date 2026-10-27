using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Starter.Template.Api.Features.Technologies;
using Xunit;

namespace Starter.Template.Api.IntegrationTests.Features.Technologies;

public class AddTechnologyTests(WebApplicationFactory<Program> factory)
    : IClassFixture<WebApplicationFactory<Program>>
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
    public async Task PostTechnology_WithMissingName_ReturnsBadRequest()
    {
        var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/v1/technologies", new { version = "1.0.0" }, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
    }
}
