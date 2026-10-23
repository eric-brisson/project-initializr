using System.ComponentModel;
using System.Diagnostics;

namespace Starter.Init;

internal static class ProcessRunner
{
    // input, when set, is written to the command's standard input (e.g. secrets kept out of the arguments).
    // A program that isn't installed gives exit code -1 instead of throwing.
    public static async Task<Result> RunAsync(
        string fileName,
        string[] arguments,
        string? input = null,
        string? workingDirectory = null,
        IReadOnlyDictionary<string, string>? environment = null)
    {
        var startInfo = new ProcessStartInfo(fileName, arguments)
        {
            RedirectStandardInput = input is not null,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            WorkingDirectory = workingDirectory ?? string.Empty,
        };

        foreach (var (key, value) in environment ?? new Dictionary<string, string>())
        {
            startInfo.Environment[key] = value;
        }

        Process process;
        try
        {
            process = Process.Start(startInfo) ?? throw new InvalidOperationException($"Impossible de lancer {fileName}.");
        }
        catch (Win32Exception)
        {
            return new Result(-1, $"{fileName} est introuvable.");
        }

        using (process)
        {
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
    }

    public sealed record Result(int ExitCode, string Output);
}
