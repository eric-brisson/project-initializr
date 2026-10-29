using System.Net;
using System.Net.Http.Json;
using Starter.Template.Api.Features.Widgets;
using Xunit;

namespace Starter.Template.Api.IntegrationTests.Features.Widgets;

public class UpdateWidgetTests(ApiFactory factory)
{
    [Fact]
    public async Task PutWidget_UpdatesIt()
    {
        var client = factory.CreateClient();
        var created = await client.PostAsJsonAsync("/api/v1/widgets", new CreateWidget.CreateWidgetRequest("Before"), TestContext.Current.CancellationToken);

        var response = await client.PutAsJsonAsync(created.Headers.Location, new UpdateWidget.UpdateWidgetRequest("After"), TestContext.Current.CancellationToken);
        var item = await client.GetFromJsonAsync<WidgetDto>(created.Headers.Location, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        Assert.Equal("After", item?.Name);
    }

    [Fact]
    public async Task PutWidget_UnknownId_ReturnsNotFound()
    {
        var client = factory.CreateClient();

        var response = await client.PutAsJsonAsync($"/api/v1/widgets/{int.MaxValue}", new UpdateWidget.UpdateWidgetRequest("After"), TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}
