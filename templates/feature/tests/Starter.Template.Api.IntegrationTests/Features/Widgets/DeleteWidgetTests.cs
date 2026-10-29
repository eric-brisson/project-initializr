using System.Net;
using System.Net.Http.Json;
using Starter.Template.Api.Features.Widgets;
using Xunit;

namespace Starter.Template.Api.IntegrationTests.Features.Widgets;

public class DeleteWidgetTests(ApiFactory factory)
{
    [Fact]
    public async Task DeleteWidget_RemovesIt()
    {
        var client = factory.CreateClient();
        var created = await client.PostAsJsonAsync("/api/v1/widgets", new CreateWidget.CreateWidgetRequest("Deleted"), TestContext.Current.CancellationToken);

        var response = await client.DeleteAsync(created.Headers.Location, TestContext.Current.CancellationToken);
        var afterDelete = await client.GetAsync(created.Headers.Location, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, afterDelete.StatusCode);
    }

    [Fact]
    public async Task DeleteWidget_UnknownId_ReturnsNotFound()
    {
        var client = factory.CreateClient();

        var response = await client.DeleteAsync($"/api/v1/widgets/{int.MaxValue}", TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}
