using System.Net;
using Xunit;

namespace Starter.Template.Api.IntegrationTests;

public class HealthCheckTests(ApiFactory factory)
{
    [Theory]
    [InlineData("/actuator/health/liveness")]
    [InlineData("/actuator/health/readiness")]
    public async Task Probe_ReturnsHealthy(string path)
    {
        var client = factory.CreateClient();

        var response = await client.GetAsync(path, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("Healthy", await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken));
    }
}
