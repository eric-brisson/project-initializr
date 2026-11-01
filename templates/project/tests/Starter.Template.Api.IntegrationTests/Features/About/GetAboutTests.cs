using System.Net.Http.Json;
using Starter.Template.Api.Features.About;
using Xunit;

namespace Starter.Template.Api.IntegrationTests.Features.About;

public class GetAboutTests(ApiFactory factory)
{
    [Fact]
    public async Task GetAbout_ReturnsTheApiAndItsComponents()
    {
        var client = factory.CreateClient();

        var about = await client.GetFromJsonAsync<GetAbout.AboutResponse>("/api/v1/about", TestContext.Current.CancellationToken);

        Assert.NotNull(about);
        Assert.Equal("Starter.Template.Api", about.Name);
        Assert.Contains(about.Components, component => component.Name == "ASP.NET Core");
    }
}
