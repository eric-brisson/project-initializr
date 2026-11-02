using System.Collections.Concurrent;
using System.Diagnostics;
using System.Net;
using System.Net.Http.Json;
using Starter.Template.Api.Common.Telemetry;
using Starter.Template.Api.Features.Technologies;
using Xunit;

namespace Starter.Template.Api.IntegrationTests;

public sealed class TelemetryTests : IDisposable
{
    private readonly ApiFactory _factory;
    private readonly ConcurrentQueue<Activity> _stopped = new();
    private readonly ActivityListener _listener;

    // A plain ActivityListener sees the custom spans: no SDK or dashboard needed to test them.
    public TelemetryTests(ApiFactory factory)
    {
        _factory = factory;
        _listener = new ActivityListener
        {
            ShouldListenTo = source => source.Name == AppTelemetry.Name,
            Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllDataAndRecorded,
            ActivityStopped = _stopped.Enqueue,
        };
        ActivitySource.AddActivityListener(_listener);
    }

    [Fact]
    public async Task CreateTechnology_RecordsASpanWithTheName()
    {
        var client = _factory.CreateClient();
        var name = $"Traced {Guid.NewGuid():N}";

        await client.PostAsJsonAsync("/api/v1/technologies", new CreateTechnology.CreateTechnologyRequest(name), TestContext.Current.CancellationToken);

        Assert.Contains(_stopped, activity => activity.OperationName == "CreateTechnology" && Equals(activity.GetTagItem("technology.name"), name));
    }

    // With the endpoint set, as in the launch profiles, the OpenTelemetry SDK is registered. Nothing listens on the
    // port here: the exports fail quietly, and the API must answer all the same.
    [Fact]
    public async Task WithTheEndpointSet_TheApiStillAnswers()
    {
        await using var factory = _factory.WithWebHostBuilder(builder =>
            builder.UseSetting(TelemetryRegistration.EndpointVariable, "http://localhost:4317"));
        var client = factory.CreateClient();

        var response = await client.GetAsync("/api/v1/about", TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    public void Dispose() => _listener.Dispose();
}
