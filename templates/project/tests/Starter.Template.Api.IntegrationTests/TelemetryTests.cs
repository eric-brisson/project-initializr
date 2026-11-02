using System.Net;
using Starter.Template.Api.Common.Telemetry;
using Xunit;

namespace Starter.Template.Api.IntegrationTests;

public class TelemetryTests(ApiFactory factory)
{
    // Avec l'adresse définie, comme dans les profils de lancement, le SDK OpenTelemetry est enregistré. Rien n'écoute sur
    // le port ici : les exports échouent en silence, et l'API doit répondre quand même.
    [Fact]
    public async Task WithTheEndpointSet_TheApiStillAnswers()
    {
        await using var telemetryFactory = factory.WithWebHostBuilder(builder =>
            builder.UseSetting(TelemetryRegistration.EndpointVariable, "http://localhost:4317"));
        var client = telemetryFactory.CreateClient();

        var response = await client.GetAsync("/api/v1/about", TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
}
