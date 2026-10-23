using System.Diagnostics;

namespace Starter.Init;

internal static class DotNetCli
{
    // "dotnet new list <name>" exits with 0 when a template matches, 103 when none does.
    public static async Task<bool> IsTemplateInstalledAsync(string shortName)
    {
        var result = await RunAsync("new", "list", shortName);
        return result.ExitCode == 0;
    }

    public static Task<Result> RunAsync(params string[] arguments) => RunAsync(arguments, input: null);

    // input, when set, is written to the command's standard input (e.g. secrets kept out of the arguments).
    public static async Task<Result> RunAsync(string[] arguments, string? input)
    {
        var startInfo = new ProcessStartInfo("dotnet", arguments)
        {
            RedirectStandardInput = input is not null,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
        };

        // Ask the dotnet CLI for French messages too, so errors match the tool's language.
        startInfo.Environment["DOTNET_CLI_UI_LANGUAGE"] = "fr";

        using var process = Process.Start(startInfo)
            ?? throw new InvalidOperationException("Impossible de lancer la CLI dotnet.");

        var standardOutput = process.StandardOutput.ReadToEndAsync();
        var standardError = process.StandardError.ReadToEndAsync();
        if (input is not null)
        {
            await process.StandardInput.WriteAsync(input);
            process.StandardInput.Close();
        }

        await process.WaitForExitAsync();

        return new Result(process.ExitCode, await standardOutput + await standardError);
    }

    public sealed record Result(int ExitCode, string Output);
}
