#if (UseDatabase)
using Microsoft.AspNetCore.Hosting;
#endif
using Microsoft.AspNetCore.Mvc.Testing;
using Starter.Template.Api.IntegrationTests;
using Xunit;

// One API (and one database) shared by every test class of the assembly.
[assembly: AssemblyFixture(typeof(ApiFactory))]

namespace Starter.Template.Api.IntegrationTests;

#if (UseDatabase)
public sealed class ApiFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly SqlServerDatabase _database = new();

    public async ValueTask InitializeAsync() => await _database.StartAsync();

    // Hides the base method, so every test goes through the Docker check before starting the API.
    public new HttpClient CreateClient()
    {
        Assert.SkipWhen(_database.SkipReason is not null, _database.SkipReason ?? string.Empty);
        return base.CreateClient();
    }

    // UseSetting (rather than ConfigureAppConfiguration) is read early enough for Program's AddDatabase.
    protected override void ConfigureWebHost(IWebHostBuilder builder) =>
        builder.UseSetting("ConnectionStrings:Default", _database.ConnectionString);

    public override async ValueTask DisposeAsync()
    {
        await base.DisposeAsync();
        await _database.DisposeAsync();
    }
}
#else
public sealed class ApiFactory : WebApplicationFactory<Program>;
#endif
