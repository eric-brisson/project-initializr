using Starter.Template.Api.Common.Telemetry;
using Xunit;

namespace Starter.Template.Api.UnitTests.Common.Telemetry;

public sealed class DashboardLauncherTests : IDisposable
{
    // solution/ avec src/Api/ en dessous, dans un dossier temporaire.
    private readonly DirectoryInfo _solution = Directory.CreateTempSubdirectory();

    private string Api => Directory.CreateDirectory(Path.Combine(_solution.FullName, "src", "Api")).FullName;

    [Fact]
    public void FindComposeDirectory_FromTheApiFolder_FindsTheSolutionFolder()
    {
        File.WriteAllText(Path.Combine(_solution.FullName, "Sample.sln"), string.Empty);
        File.WriteAllText(Path.Combine(_solution.FullName, "compose.yaml"), string.Empty);

        Assert.Equal(_solution.FullName, DashboardLauncher.FindComposeDirectory(Api));
    }

    [Fact]
    public void FindComposeDirectory_DoesNotLookAboveTheSolutionFolder()
    {
        var solution = Directory.CreateDirectory(Path.Combine(_solution.FullName, "inner"));
        File.WriteAllText(Path.Combine(solution.FullName, "Sample.sln"), string.Empty);
        File.WriteAllText(Path.Combine(_solution.FullName, "compose.yaml"), string.Empty);
        var api = Directory.CreateDirectory(Path.Combine(solution.FullName, "src", "Api")).FullName;

        Assert.Null(DashboardLauncher.FindComposeDirectory(api));
    }

    public void Dispose() => _solution.Delete(recursive: true);
}
