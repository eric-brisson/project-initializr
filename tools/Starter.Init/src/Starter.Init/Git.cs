namespace Starter.Init;

internal static class Git
{
    // False when Git isn't installed (exit code -1) or the folder is already in a repo (e.g. a freshly cloned one).
    // The folder may not exist yet: its nearest existing parent decides.
    public static async Task<bool> CanInitAsync(string folder)
    {
        var existing = new DirectoryInfo(folder);
        while (existing is { Exists: false })
        {
            existing = existing.Parent;
        }

        var result = await ProcessRunner.RunAsync(
            "git", ["rev-parse", "--is-inside-work-tree"], workingDirectory: existing?.FullName);
        return result.ExitCode != -1 && result.Output.Trim() != "true";
    }

    // git commit fails without user.name and user.email (usually set in the global config).
    public static async Task<bool> HasIdentityAsync() =>
        (await ProcessRunner.RunAsync("git", ["config", "--get", "user.name"])).ExitCode == 0
        && (await ProcessRunner.RunAsync("git", ["config", "--get", "user.email"])).ExitCode == 0;

    // .env is unstaged explicitly: it holds the sa password, and a .gitignore kept from before may not ignore it.
    // (An exclude pathspec like ':!.env' makes git add fail when .gitignore already ignores the file.)
    public static async Task<ProcessRunner.Result> CommitAllAsync(string folder, string message)
    {
        string[][] commands =
        [
            ["add", "--all"],
            ["rm", "--cached", "--quiet", "--ignore-unmatch", "--", ".env"],
            ["commit", "--quiet", "--message", message],
        ];

        ProcessRunner.Result result = new(0, string.Empty);
        foreach (var arguments in commands)
        {
            result = await ProcessRunner.RunAsync("git", arguments, workingDirectory: folder);
            if (result.ExitCode != 0)
            {
                break;
            }
        }

        return result;
    }

    // main whatever init.defaultBranch says (older Git versions still default to master).
    public static Task<ProcessRunner.Result> InitAsync(string folder) =>
        ProcessRunner.RunAsync("git", ["init", "--initial-branch=main"], workingDirectory: folder);
}
