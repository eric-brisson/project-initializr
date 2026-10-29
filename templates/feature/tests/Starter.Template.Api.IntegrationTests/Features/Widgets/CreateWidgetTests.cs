using System.Net;
using System.Net.Http.Json;
using Starter.Template.Api.Features.Widgets;
using Xunit;

namespace Starter.Template.Api.IntegrationTests.Features.Widgets;

public class CreateWidgetTests(ApiFactory factory)
{
    [Fact]
    public async Task PostWidget_ReturnsCreatedWithItsLocation()
    {
        var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/v1/widgets", new CreateWidget.CreateWidgetRequest("Created"), TestContext.Current.CancellationToken);
        var item = await response.Content.ReadFromJsonAsync<WidgetDto>(TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.NotNull(item);
        Assert.Equal("Created", item.Name);
        Assert.Equal($"/api/v1/widgets/{item.Id}", response.Headers.Location?.AbsolutePath);
    }

    [Fact]
    public async Task PostWidget_WithMissingName_ReturnsBadRequest()
    {
        var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/v1/widgets", new { }, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
}
