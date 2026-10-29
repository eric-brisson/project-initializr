using System.Net;
using System.Net.Http.Json;
using Starter.Template.Api.Features.Widgets;
using Xunit;

namespace Starter.Template.Api.IntegrationTests.Features.Widgets;

public class GetWidgetTests(ApiFactory factory)
{
    [Fact]
    public async Task GetWidget_ReturnsIt()
    {
        var client = factory.CreateClient();
        var created = await client.PostAsJsonAsync("/api/v1/widgets", new CreateWidget.CreateWidgetRequest("Found"), TestContext.Current.CancellationToken);

        var response = await client.GetAsync(created.Headers.Location, TestContext.Current.CancellationToken);
        var item = await response.Content.ReadFromJsonAsync<WidgetDto>(TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("Found", item?.Name);
    }

    [Fact]
    public async Task GetWidget_UnknownId_ReturnsNotFound()
    {
        var client = factory.CreateClient();

        var response = await client.GetAsync($"/api/v1/widgets/{int.MaxValue}", TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}
