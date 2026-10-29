using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Containers;
using DotNet.Testcontainers.Networks;
using Microsoft.Data.SqlClient;
using Testcontainers.MsSql;

namespace Starter.Template.Api.IntegrationTests;

// A throwaway SQL Server in Docker, migrated by the same Flyway image and scripts as compose.yaml.
// Without Docker, the tests are skipped, except on a CI server (CI is set by GitHub Actions) where they fail.
internal sealed class SqlServerDatabase : IAsyncDisposable
{
    private const string DatabaseName = "StarterTemplate";
    private const string FlywayImage = "flyway/flyway:13.10";

    private INetwork? _network;
    private MsSqlContainer? _sqlServer;

    public string ConnectionString { get; private set; } = string.Empty;

    public string? SkipReason { get; private set; }

    public async Task StartAsync()
    {
        try
        {
            _network = new NetworkBuilder().Build();
            _sqlServer = new MsSqlBuilder("mcr.microsoft.com/mssql/server:2022-latest")
                .WithNetwork(_network)
                .WithNetworkAliases("sqlserver")
                .Build();

            await _sqlServer.StartAsync();
        }
        catch (DockerUnavailableException exception) when (Environment.GetEnvironmentVariable("CI") is null)
        {
            SkipReason = $"Docker n'est pas disponible : {exception.Message}";
            return;
        }

        var connection = new SqlConnectionStringBuilder(_sqlServer.GetConnectionString());
        await CreateDatabaseAsync(connection.ConnectionString);
        await MigrateAsync(connection.Password);

        connection.InitialCatalog = DatabaseName;
        ConnectionString = connection.ConnectionString;
    }

    public async ValueTask DisposeAsync()
    {
        if (_sqlServer is not null)
        {
            await _sqlServer.DisposeAsync();
        }

        if (_network is not null)
        {
            await _network.DisposeAsync();
        }
    }

    private static async Task CreateDatabaseAsync(string masterConnectionString)
    {
        await using var connection = new SqlConnection(masterConnectionString);
        await connection.OpenAsync();

        await using var command = new SqlCommand($"CREATE DATABASE [{DatabaseName}]", connection);
        await command.ExecuteNonQueryAsync();
    }

    // The scripts are copied next to the test assembly (see the .csproj), then into the Flyway container.
    private async Task MigrateAsync(string saPassword)
    {
        await using var flyway = new ContainerBuilder(FlywayImage)
            .WithNetwork(_network)
            .WithResourceMapping(new DirectoryInfo(Path.Combine(AppContext.BaseDirectory, "migrations")), "/flyway/sql")
            .WithEnvironment("FLYWAY_URL", $"jdbc:sqlserver://sqlserver:1433;databaseName={DatabaseName};encrypt=true;trustServerCertificate=true")
            .WithEnvironment("FLYWAY_USER", "sa")
            .WithEnvironment("FLYWAY_PASSWORD", saPassword)
            .WithCommand("migrate")
            .Build();

        await flyway.StartAsync();

        var exitCode = await flyway.GetExitCodeAsync();
        if (exitCode != 0)
        {
            var (stdout, stderr) = await flyway.GetLogsAsync();
            throw new InvalidOperationException($"Flyway failed with exit code {exitCode}:\n{stdout}\n{stderr}");
        }
    }
}
