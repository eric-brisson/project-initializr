using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using Spectre.Console;
using Starter.Init.Prompts;

namespace Starter.Init;

internal static class Program
{
    public static async Task<int> Main(string[] args)
    {
        // Windows consoles default to a legacy code page without symbols like ◆ and ✔.
        Console.OutputEncoding = Encoding.UTF8;

        // The prompts hide the cursor: bring it back if the user quits with Ctrl+C.
        Console.CancelKeyPress += (_, _) => AnsiConsole.Cursor.Show();

        ShowIntro();

        // Arguments skip every prompt (for scripts): starter-init <name> [folder].
        // The first template and its default modules are used.
        var interactive = args.Length == 0;
        if (interactive && !Keyboard.IsAvailable)
        {
            Ui.Fail("Pas de clavier disponible : passez le nom en argument, par exemple starter-init Sample.Orders");
            return 1;
        }

        // The template comes first: the other questions depend on the project type.
        var template = interactive
            ? new SelectPrompt<Templates.Choice>(Templates.All, choice => choice.Name, choice => choice.Description)
                .Ask("modèle", "Quel modèle voulez-vous utiliser ?")
            : Templates.All[0];

        var outputDir = interactive ? AskOutputDir() : null;
        var name = interactive ? AskName(outputDir!) : args[0];

        if (!ValidName().IsMatch(name))
        {
            Ui.Fail($"'{name}' n'est pas un espace de noms C# valide, par exemple Sample.Orders");
            return 1;
        }

        if (!interactive)
        {
            outputDir = args.Length > 1 ? ToFullPath(args[1]) : DefaultOutputDir(name);
            if (outputDir is null)
            {
                Ui.Fail($"'{args[1]}' n'est pas un chemin valide.");
                return 1;
            }
        }

        var modules = interactive && template.Modules.Count > 0
            ? new MultiSelectPrompt<Templates.Module>(
                    template.Modules,
                    module => module.Category,
                    module => module.Name,
                    module => module.Description,
                    module => module.SelectedByDefault)
                .Ask("modules", "Quels modules voulez-vous ajouter ?")
            : [.. template.Modules.Where(module => module.SelectedByDefault)];

        if (Directory.Exists(Path.Combine(outputDir!, "src", $"{name}.Api")))
        {
            Ui.Fail($"src\\{name}.Api existe déjà dans {outputDir}.");
            return 1;
        }

        if (!await DotNetCli.IsTemplateInstalledAsync(template.ShortName))
        {
            Ui.Fail($"Le modèle '{template.ShortName}' n'est pas installé.");
            Ui.Line("[grey]Depuis le dépôt du modèle, lancez : dotnet new install .[/]");
            return 1;
        }

        if (interactive && !IsEmptyFolder(outputDir!)
            && !AskYesNo("dossier", "Le dossier n'est pas vide. Continuer quand même ?", yesFirst: false))
        {
            return 0;
        }

        // dotnet new refuses to overwrite files, and a freshly cloned repo often has its own .gitignore.
        var gitignore = Path.Combine(outputDir!, ".gitignore");
        var keepGitignore = File.Exists(gitignore)
            && (!interactive || !AskYesNo("git", "Un .gitignore existe déjà. Le remplacer par celui du modèle ?", yesFirst: false));
        var replaceGitignore = File.Exists(gitignore) && !keepGitignore;

        if (interactive && !AskYesNo("créer", "Tout est prêt. Générer le projet ?"))
        {
            return 0;
        }

        var solution = Path.Combine(outputDir!, $"{name}.sln");
        Step[] steps =
        [
            new("Génération du projet", "Projet généré", Color.FromHex("#8B9BFA"),
            [
                "new", template.ShortName, "--name", name, "--output", outputDir!,
                .. ModuleArguments(template, modules),
                .. (replaceGitignore ? ["--force"] : Array.Empty<string>()),
            ]),
            new("Restauration des paquets", "Paquets restaurés", Color.FromHex("#98C379"), ["restore", solution]),
            new("Compilation de la solution", "Solution compilée", Color.FromHex("#61AFEF"), ["build", solution, "--no-restore"]),
            new("Exécution des tests", "Tests réussis", Color.FromHex("#E5C07B"), ["test", solution, "--no-build"]),
        ];

        // Keeping the existing .gitignore: move it aside while generating, then put it back.
        var gitignoreBackup = gitignore + ".starter-init.bak";
        if (keepGitignore)
        {
            File.Move(gitignore, gitignoreBackup);
        }

        bool succeeded;
        try
        {
            Ui.Badge("étapes", "Création du projet");
            succeeded = await RunStepsAsync(steps);
        }
        finally
        {
            if (keepGitignore)
            {
                File.Move(gitignoreBackup, gitignore, overwrite: true);
            }
        }

        if (!succeeded)
        {
            return 1;
        }

        ShowNextSteps(name, outputDir!, modules);
        return 0;
    }

    private sealed record Step(string Running, string Done, Color Color, string[] Arguments);

    // One "--modules <id>" per selected module; an empty value turns off the template's default modules.
    private static string[] ModuleArguments(Templates.Choice template, IReadOnlyList<Templates.Module> modules) =>
        template.Modules.Count == 0 ? []
        : modules.Count == 0 ? ["--modules", string.Empty]
        : [.. modules.SelectMany(module => new[] { "--modules", module.Id })];

    // Shows every step up front, then runs them one by one, each in its own color.
    private static async Task<bool> RunStepsAsync(IReadOnlyList<Step> steps)
    {
        var display = new StepDisplay();
        var finalLines = new List<string>();
        DotNetCli.Result? failure = null;

        // Pad labels to the longest one so the durations stay in place when a label changes.
        // The padding sits inside the color tag, because Spectre trims the description.
        var labelWidth = steps.Max(step => Math.Max(step.Done.Length, $"{step.Running} a échoué".Length));
        string Label(string text, string color) => $"[{color}]{text.PadRight(labelWidth)}[/]";

        await AnsiConsole.Progress()
            .AutoClear(false)
            .Columns(display.Columns)
            .StartAsync(async context =>
            {
                var tasks = steps
                    .Select(step => context.AddTask(Label(step.Running, "grey"), autoStart: false))
                    .ToList();

                for (var i = 0; i < steps.Count; i++)
                {
                    var (step, task) = (steps[i], tasks[i]);
                    var color = step.Color.ToMarkup();
                    task.Description = Label(step.Running, color);
                    task.StartTask();

                    var result = await DotNetCli.RunAsync(step.Arguments);
                    if (result.ExitCode != 0)
                    {
                        display.MarkFailed(task);
                        task.Description = Label($"{step.Running} a échoué", "red");
                        task.StopTask();
                        finalLines.Add($"[red]✘ {step.Running} a échoué[/]");
                        failure = result;
                        return;
                    }

                    task.Description = Label(step.Done, color);
                    task.StopTask();
                    finalLines.Add($"[green]✔[/] [{color}]{step.Done}[/] [grey]({task.ElapsedTime?.TotalSeconds:0.0} s)[/]");
                }
            });

        // Without a real terminal (script, redirected output), Spectre draws nothing: print the outcome instead.
        if (!AnsiConsole.Profile.Capabilities.Interactive)
        {
            finalLines.ForEach(Ui.Line);
        }

        // The dotnet output is printed after the live display, so it doesn't fight with the redraws.
        if (failure is not null)
        {
            AnsiConsole.WriteLine();
            AnsiConsole.WriteLine(failure.Output);
            return false;
        }

        return true;
    }

    private static void ShowIntro()
    {
        var version = typeof(Program).Assembly
            .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion
            .Split('+')[0];

        Ui.Badge("starter", $"Bienvenue dans Starter Init ! [grey](v{version})[/]", Ui.Success);
    }

    private static string AskOutputDir()
    {
        var current = Directory.GetCurrentDirectory();
        var suggestion = IsEmptyFolder(current) ? "." : Path.Combine(".", "Sample.Orders");

        var folder = new InputPrompt(
                suggestion,
                validate: path => ToFullPath(path) is null ? "Chemin invalide" : null,
                hint: path => ToFullPath(path),
                summary: path => ToFullPath(path)!)
            .Ask("dossier", "Où voulez-vous créer le projet ?");

        return ToFullPath(folder)!;
    }

    private static string AskName(string outputDir) =>
        new InputPrompt(
                NameFromFolder(outputDir),
                validate: name => ValidName().IsMatch(name)
                    ? null
                    : "Utilisez un espace de noms C# valide, par exemple Sample.Orders",
                hint: name => ValidName().IsMatch(name)
                    ? $"Le projet API sera {Path.Combine("src", $"{name}.Api")}"
                    : null)
            .Ask("nom", "Quel nom pour la solution ?");

    private static bool AskYesNo(string badge, string question, bool yesFirst = true)
    {
        (string Label, bool Value)[] choices = yesFirst ? [("Oui", true), ("Non", false)] : [("Non", false), ("Oui", true)];
        return new SelectPrompt<(string Label, bool Value)>(choices, choice => choice.Label)
            .Ask(badge, question)
            .Value;
    }

    private static void ShowNextSteps(string name, string outputDir, IReadOnlyList<Templates.Module> modules)
    {
        // The step list already ends with a blank line.
        Ui.Line($"[{Ui.Accent.ToMarkup()}]✔[/] [{Ui.Success.ToMarkup()}]Projet initialisé ![/]");

        Ui.Badge("suite", "C'est parti ! Explorez votre projet.", Ui.Info);
        if (outputDir != Directory.GetCurrentDirectory())
        {
            Ui.Line($"Entrez dans le dossier avec [#61AFEF]cd \"{Markup.Escape(outputDir)}\"[/]");
        }

        Ui.Line($"Lancez l'API avec [#61AFEF]dotnet run --project {Markup.Escape(Path.Combine("src", $"{name}.Api"))}[/]");
        foreach (var module in modules.Where(module => module.NextStep is not null))
        {
            Ui.Line(module.NextStep!);
        }

        AnsiConsole.WriteLine();
    }

    // "sample-orders" → "Sample.Orders". A trailing "Api" is dropped, since the project name adds ".Api".
    private static string NameFromFolder(string folder)
    {
        var parts = Path.GetFileName(Path.TrimEndingDirectorySeparator(folder))
            .Split(['-', '_', '.', ' '], StringSplitOptions.RemoveEmptyEntries)
            .Select(part => new string([.. part.Where(char.IsLetterOrDigit)]))
            .Where(part => part.Length > 0)
            .Select(part => char.ToUpperInvariant(part[0]) + part[1..])
            .ToList();

        if (parts.Count > 1 && parts[^1] == "Api")
        {
            parts.RemoveAt(parts.Count - 1);
        }

        var name = string.Join('.', parts);
        return ValidName().IsMatch(name) ? name : "Sample.Orders";
    }

    // The current folder when it's empty (e.g. a freshly cloned repo), otherwise a new <name> subfolder.
    private static string DefaultOutputDir(string name)
    {
        var current = Directory.GetCurrentDirectory();
        return IsEmptyFolder(current) ? current : Path.Combine(current, name);
    }

    // Relative paths are resolved from the current folder; returns null for an invalid path or an existing file.
    private static string? ToFullPath(string path)
    {
        try
        {
            var fullPath = Path.GetFullPath(path.Trim().Trim('"'), Directory.GetCurrentDirectory());
            return File.Exists(fullPath) ? null : fullPath;
        }
        catch (Exception exception) when (exception is ArgumentException or NotSupportedException or PathTooLongException)
        {
            return null;
        }
    }

    // A folder counts as empty when it doesn't exist or only holds repo files (.git, .gitignore, README, LICENSE).
    private static bool IsEmptyFolder(string path) =>
        !Directory.Exists(path)
        || Directory.EnumerateFileSystemEntries(path)
            .Select(Path.GetFileName)
            .All(entry => entry is not null
                && (entry.StartsWith('.')
                    || entry.StartsWith("README", StringComparison.OrdinalIgnoreCase)
                    || entry.StartsWith("LICENSE", StringComparison.OrdinalIgnoreCase)));

    private static readonly Regex ValidNamePattern =
        new(@"^[A-Za-z_][A-Za-z0-9_]*(\.[A-Za-z_][A-Za-z0-9_]*)*$", RegexOptions.Compiled);

    private static Regex ValidName() => ValidNamePattern;
}
