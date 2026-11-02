#if (UseDatabase)
using Microsoft.AspNetCore.Hosting;
#endif
using Microsoft.AspNetCore.Mvc.Testing;
using Starter.Template.Api.IntegrationTests;
using Xunit;

// Une seule API (et une seule base) partagée par toutes les classes de test de l'assembly.
[assembly: AssemblyFixture(typeof(ApiFactory))]

namespace Starter.Template.Api.IntegrationTests;

#if (UseDatabase)
public sealed class ApiFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly SqlServerDatabase _database = new();

    public async ValueTask InitializeAsync() => await _database.StartAsync();

    // Masque la méthode de base : chaque test passe par la vérification de Docker avant de démarrer l'API.
    public new HttpClient CreateClient()
    {
        Assert.SkipWhen(_database.SkipReason is not null, _database.SkipReason ?? string.Empty);
        return base.CreateClient();
    }

    // UseSetting (plutôt que ConfigureAppConfiguration) est lu assez tôt pour le AddDatabase de Program.
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
