namespace Starter.Init;

// The SQL Server container of the database module (compose.yaml in the generated project).
internal static class Docker
{
    public const string SqlServerImage = "mcr.microsoft.com/mssql/server:2022-latest";

    public const int DefaultSqlPort = 1433;

    // Where the API sends its telemetry: the OTLP port of the dashboard service of compose.yaml.
    public const int OtlpPort = 4317;

    // "docker info" fails when Docker isn't installed or its daemon (Docker Desktop) isn't running.
    public static async Task<bool> IsAvailableAsync() =>
        (await ProcessRunner.RunAsync("docker", ["info", "--format", "{{.ServerVersion}}"])).ExitCode == 0;

    // Checked every 2 seconds until the timeout.
    public static async Task<bool> WaitUntilAvailableAsync(TimeSpan timeout)
    {
        var deadline = DateTime.UtcNow + timeout;
        while (!await IsAvailableAsync())
        {
            if (DateTime.UtcNow >= deadline)
            {
                return false;
            }

            await Task.Delay(TimeSpan.FromSeconds(2));
        }

        return true;
    }

    // Docker Desktop on macOS and Windows. On Linux, Docker usually runs as a system service the tool can't start.
    public static bool CanStartDesktop => DesktopApp is not null;

    // Opens the app and returns right away: the engine is ready only later (see WaitUntilAvailableAsync).
    public static async Task<bool> StartDesktopAsync() =>
        DesktopApp is { } app
        && (OperatingSystem.IsMacOS()
            ? (await ProcessRunner.RunAsync("open", ["-a", app])).ExitCode == 0
            : StartDetached(app));

    private static string? DesktopApp =>
        OperatingSystem.IsMacOS()
            ? new[] { "/Applications/Docker.app", Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Applications", "Docker.app") }
                .FirstOrDefault(Directory.Exists)
            : OperatingSystem.IsWindows()
                ? new[] { Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Docker", "Docker", "Docker Desktop.exe") }
                    .FirstOrDefault(File.Exists)
                : null;

    private static bool StartDetached(string path)
    {
        try
        {
            using var _ = System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(path) { UseShellExecute = true });
            return true;
        }
        catch (System.ComponentModel.Win32Exception)
        {
            return false;
        }
    }

    // 1433 when it's free, otherwise the first free port from 14330: another SQL Server often holds 1433.
    public static int FindSqlPort() =>
        new[] { DefaultSqlPort }.Concat(Enumerable.Range(14330, 10)).FirstOrDefault(IsPortFree, DefaultSqlPort);

    // --wait returns once the container's healthcheck passes, so the database can be created right after.
    // The first run also downloads the image. Only the sqlserver service: compose.yaml also builds and runs the API.
    public static Task<ProcessRunner.Result> StartSqlServerAsync(string projectDir) =>
        ProcessRunner.RunAsync("docker", ["compose", "up", "--detach", "--wait", "sqlserver"], workingDirectory: projectDir);

    // The Aspire dashboard of the telemetry module. The first run also downloads the image.
    public static Task<ProcessRunner.Result> StartDashboardAsync(string projectDir) =>
        ProcessRunner.RunAsync("docker", ["compose", "up", "--detach", "dashboard"], workingDirectory: projectDir);

    // The password reaches sqlcmd through SQLCMDPASSWORD ("-e" without a value copies it from this process),
    // so it isn't in any command line. The name is a validated identifier (letters, digits, _), safe inside [ ] and N' '.
    public static Task<ProcessRunner.Result> CreateDatabaseAsync(string projectDir, string database, string saPassword) =>
        ProcessRunner.RunAsync(
            "docker",
            [
                "compose", "exec", "-T", "-e", "SQLCMDPASSWORD", "sqlserver",
                "/opt/mssql-tools18/bin/sqlcmd", "-S", "localhost", "-U", "sa", "-C", "-b",
                "-Q", $"IF DB_ID(N'{database}') IS NULL CREATE DATABASE [{database}]",
            ],
            workingDirectory: projectDir,
            environment: new Dictionary<string, string> { ["SQLCMDPASSWORD"] = saPassword });

    // The flyway service of compose.yaml applies the scripts of db/migrations to the database created above.
    // "run" starts it even though it sits in a compose profile, and --rm removes the container once done.
    public static Task<ProcessRunner.Result> MigrateAsync(string projectDir) =>
        ProcessRunner.RunAsync("docker", ["compose", "run", "--rm", "flyway"], workingDirectory: projectDir);

    // Taken when it can't be bound, or when something already answers on the loopback.
    public static bool IsPortFree(int port)
    {
        try
        {
            var listener = new TcpListener(IPAddress.Any, port);
            listener.Start();
            listener.Stop();
        }
        catch (SocketException)
        {
            return false;
        }

        try
        {
            using var client = new TcpClient();
            client.Connect(IPAddress.Loopback, port);
            return false;
        }
        catch (SocketException)
        {
            return true;
        }
    }
}
