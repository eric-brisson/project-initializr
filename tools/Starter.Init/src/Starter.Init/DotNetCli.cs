namespace Starter.Init;

internal static class DotNetCli
{
    // Ask the dotnet CLI for French messages too, so errors match the tool's language.
    private static readonly Dictionary<string, string> Environment = new() { ["DOTNET_CLI_UI_LANGUAGE"] = "fr" };

    // "dotnet new list <name>" exits with 0 when a template matches, 103 when none does.
    public static async Task<bool> IsTemplateInstalledAsync(string shortName)
    {
        var result = await RunAsync("new", "list", shortName);
        return result.ExitCode == 0;
    }

    public static Task<ProcessRunner.Result> RunAsync(params string[] arguments) => RunAsync(arguments, input: null);

    public static Task<ProcessRunner.Result> RunAsync(string[] arguments, string? input) =>
        ProcessRunner.RunAsync("dotnet", arguments, input, environment: Environment);
}
