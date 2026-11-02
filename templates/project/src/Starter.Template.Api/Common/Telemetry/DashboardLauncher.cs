using System.ComponentModel;
using System.Diagnostics;
using System.Net.Sockets;

namespace Starter.Template.Api.Common.Telemetry;

// Confort de développement : quand l'API démarre depuis un profil de lancement (F5 dans l'IDE, dotnet run) et que rien
// ne répond à l'adresse de télémétrie, démarre le tableau de bord Aspire de compose.yaml. En arrière-plan : l'API
// n'attend pas Docker (la première fois, le téléchargement de l'image prend une minute, et la télémétrie de cette
// minute est perdue). Sans Docker, un avertissement donne la commande à lancer plus tard, et l'API fonctionne quand
// même : ses exports de télémétrie échouent en silence jusqu'à ce qu'un tableau de bord écoute.
internal sealed partial class DashboardLauncher(
    IConfiguration configuration,
    IHostEnvironment environment,
    ILogger<DashboardLauncher> logger) : BackgroundService
{
    // Mis à true par les profils de lancement (launchSettings.json) : les tests et le conteneur de l'API ne démarrent jamais Docker.
    public const string EnabledKey = "Telemetry:StartDashboard";

    // Le port de l'interface du service dashboard de compose.yaml.
    private const string DashboardUrl = "http://localhost:18888";

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Seulement pour un tableau de bord sur cette machine, et seulement si rien n'écoute encore : un tableau de bord
        // démarré à la main, par starter-init ou par un autre projet reçoit la télémétrie tout aussi bien.
        if (!Uri.TryCreate(configuration[TelemetryRegistration.EndpointVariable], UriKind.Absolute, out var endpoint)
            || !endpoint.IsLoopback
            || await IsListeningAsync(endpoint, stoppingToken))
        {
            return;
        }

        var composeDirectory = FindComposeDirectory(environment.ContentRootPath);
        if (composeDirectory is null)
        {
            LogNoComposeFile(environment.ContentRootPath);
            return;
        }

        LogStarting();
        var (exitCode, output) = await RunDockerComposeAsync(composeDirectory, stoppingToken);
        if (exitCode == 0)
        {
            LogStarted(DashboardUrl);
        }
        else
        {
            LogFailed(output.Trim());
        }
    }

    // compose.yaml est dans le dossier de la solution : on remonte du dossier de l'API (le content root) jusqu'à celui du
    // .sln, pas plus haut, où pourrait se trouver le compose.yaml d'un autre projet.
    internal static string? FindComposeDirectory(string contentRoot)
    {
        for (var directory = new DirectoryInfo(contentRoot); directory is not null; directory = directory.Parent)
        {
            if (File.Exists(Path.Combine(directory.FullName, "compose.yaml")))
            {
                return directory.FullName;
            }

            if (directory.EnumerateFiles("*.sln*").Any())
            {
                return null;
            }
        }

        return null;
    }

    private static async Task<bool> IsListeningAsync(Uri endpoint, CancellationToken cancellationToken)
    {
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(TimeSpan.FromMilliseconds(500));

        try
        {
            using var client = new TcpClient();
            await client.ConnectAsync(endpoint.Host, endpoint.Port, timeout.Token);
            return true;
        }
        catch (Exception exception) when (exception is SocketException or OperationCanceledException)
        {
            return false;
        }
    }

    private static async Task<(int ExitCode, string Output)> RunDockerComposeAsync(string directory, CancellationToken cancellationToken)
    {
        var startInfo = new ProcessStartInfo("docker", ["compose", "up", "--detach", "dashboard"])
        {
            WorkingDirectory = directory,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
        };

        try
        {
            using var process = Process.Start(startInfo)!;
            var output = process.StandardOutput.ReadToEndAsync(cancellationToken);
            var error = process.StandardError.ReadToEndAsync(cancellationToken);
            await process.WaitForExitAsync(cancellationToken);

            return (process.ExitCode, await output + await error);
        }
        catch (Win32Exception)
        {
            return (-1, "The docker command wasn't found: is Docker installed?");
        }
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "Starting the Aspire dashboard in Docker (docker compose up -d dashboard)...")]
    private partial void LogStarting();

    [LoggerMessage(Level = LogLevel.Information, Message = "Aspire dashboard started: {Url}")]
    private partial void LogStarted(string url);

    [LoggerMessage(Level = LogLevel.Warning, Message = "The Aspire dashboard couldn't start. Start Docker, then run: docker compose up -d dashboard\n{Output}")]
    private partial void LogFailed(string output);

    [LoggerMessage(Level = LogLevel.Warning, Message = "No compose.yaml found from {ContentRoot} up to the solution folder: the Aspire dashboard isn't started.")]
    private partial void LogNoComposeFile(string contentRoot);
}
