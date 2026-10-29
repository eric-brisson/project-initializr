using System.Net.Http.Json;
using Starter.Template.Api.Features.Widgets;
using Xunit;

namespace Starter.Template.Api.IntegrationTests.Features.Widgets;

public class ListWidgetsTests(ApiFactory factory)
{
    [Fact]
    public async Task GetWidgets_ReturnsTheCreatedWidget()
    {
        var client = factory.CreateClient();
        var created = await client.PostAsJsonAsync("/api/v1/widgets", new CreateWidget.CreateWidgetRequest("Listed"), TestContext.Current.CancellationToken);
        var item = await created.Content.ReadFromJsonAsync<WidgetDto>(TestContext.Current.CancellationToken);

        var items = await client.GetFromJsonAsync<List<WidgetDto>>("/api/v1/widgets", TestContext.Current.CancellationToken);

        Assert.NotNull(items);
        Assert.Contains(item, items);
    }
}
