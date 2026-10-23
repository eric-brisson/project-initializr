using System.Reflection;
using System.Text;
using System.Text.Json;
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
            Ui.Fail("Ce terminal ne permet pas les questions interactives.");
            Ui.Line("[grey]Lancez l'outil dans un vrai terminal (Windows Terminal, PowerShell), pas dans la fenêtre « Run » d'un IDE,[/]");
            Ui.Line("[grey]ou passez le nom en argument, par exemple : starter-init Sample.Orders[/]");
            return 1;
        }

        // Spectre turns live displays off when it sees a build server variable (TF_BUILD, GITHUB_ACTIONS,
        // JENKINS_URL, TEAMCITY_VERSION), even in a real terminal. With a real keyboard and screen, force them on.
        if (interactive)
        {
            AnsiConsole.Profile.Capabilities.Interactive = true;
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

        var database = interactive && modules.Any(module => module.Id == Templates.DatabaseId)
            ? await PrepareDatabaseAsync(AskDatabaseName(name), AskSaPassword())
            : null;

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
            && !AskYesNo("dossier", $"Le dossier [#61AFEF]{Markup.Escape(outputDir!)}[/] n'est pas vide. Continuer quand même ?", yesFirst: false))
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
            new("Génération du projet", "Projet généré", Color.FromHex("#8B9BFA"), () => DotNetCli.RunAsync(
            [
                "new", template.ShortName, "--name", name, "--output", outputDir!,
                .. ModuleArguments(template, modules),
                .. (database is not null ? ["--databaseName", database.Name] : Array.Empty<string>()),
                .. (replaceGitignore ? ["--force"] : Array.Empty<string>()),
            ])),
            new("Restauration des paquets", "Paquets restaurés", Color.FromHex("#98C379"), () => DotNetCli.RunAsync("restore", solution)),
            .. (database is not null
                ? [new Step("Configuration de la base", "Base configurée", Color.FromHex("#C678DD"),
                    () => ConfigureDatabaseAsync(outputDir!, name, database))]
                : Array.Empty<Step>()),
            new("Compilation de la solution", "Solution compilée", Color.FromHex("#61AFEF"), () => DotNetCli.RunAsync("build", solution, "--no-restore")),
            new("Exécution des tests", "Tests réussis", Color.FromHex("#E5C07B"), () => DotNetCli.RunAsync("test", solution, "--no-build")),

            // Last, so a Docker problem (image download, port) doesn't get in the way of the build and tests.
            .. (database is { UseDocker: true }
                ?
                [
                    new Step("Démarrage de SQL Server dans Docker", "SQL Server démarré dans Docker", Ui.Docker,
                        () => Docker.StartSqlServerAsync(outputDir!)),
                    new Step($"Création de la base {database.Name}", $"Base {database.Name} créée", Ui.Docker,
                        () => Docker.CreateDatabaseAsync(outputDir!, database.Name, database.SaPassword)),
                ]
                : Array.Empty<Step>()),
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

        ShowNextSteps(name, outputDir!, modules, database);
        return 0;
    }

    private sealed record Step(string Running, string Done, Color Color, Func<Task<ProcessRunner.Result>> Run);

    // UseDocker is false when Docker isn't available: the project is still configured, the container just isn't started.
    private sealed record Database(string Name, string SaPassword, int Port, bool UseDocker);

    // One "--modules <id>" per selected module; an empty value turns off the template's default modules.
    private static string[] ModuleArguments(Templates.Choice template, IReadOnlyList<Templates.Module> modules) =>
        template.Modules.Count == 0 ? []
        : modules.Count == 0 ? ["--modules", string.Empty]
        : [.. modules.SelectMany(module => new[] { "--modules", module.Id })];

    // Tells up front what happens with Docker, since starting SQL Server is the slowest step (image download).
    private static async Task<Database> PrepareDatabaseAsync(string databaseName, string saPassword)
    {
        var port = Docker.FindSqlPort();
        var portInfo = port == Docker.DefaultSqlPort ? $"port {port}" : $"port {port}, car 1433 est déjà utilisé";

        var useDocker = await Docker.IsAvailableAsync();
        if (useDocker)
        {
            Ui.Badge("docker", "SQL Server sera démarré dans un conteneur Docker", Ui.Docker);
            Ui.Line($"[grey]Image {Docker.SqlServerImage} (téléchargée au premier lancement), {portInfo}[/]");
        }
        else
        {
            Ui.Badge("docker", $"[{Ui.Warning.ToMarkup()}]Docker n'est pas disponible : SQL Server ne sera pas démarré[/]", Ui.Warning);
            Ui.Line($"[grey]Le projet sera configuré quand même ({portInfo}). Lancez Docker Desktop, puis docker compose up -d.[/]");
        }

        return new Database(databaseName, saPassword, port, useDocker);
    }

    // The API reads the full connection string from its user secrets; docker compose reads the password from .env.
    // The secrets go through stdin, so the password doesn't show up in the process list.
    private static async Task<ProcessRunner.Result> ConfigureDatabaseAsync(string outputDir, string name, Database database)
    {
        var saPassword = database.SaPassword;

        // Quoted, so a ; in the password doesn't end the value.
        var connectionString =
            $"Server=localhost,{database.Port};Database={database.Name};User Id=sa;Password=\"{saPassword.Replace("\"", "\"\"")}\";TrustServerCertificate=True";
        var secrets = JsonSerializer.Serialize(new { ConnectionStrings = new { Default = connectionString } });

        var project = Path.Combine(outputDir, "src", $"{name}.Api");
        var result = await DotNetCli.RunAsync(["user-secrets", "set", "--project", project], secrets);
        if (result.ExitCode != 0)
        {
            return result;
        }

        // Added to an existing .env (a cloned repo may have one) rather than replacing it.
        // Single quotes keep the value literal: docker compose doesn't expand $ in it.
        await File.AppendAllTextAsync(
            Path.Combine(outputDir, ".env"),
            $"# sa password of the SQL Server container (compose.yaml). Not committed.\nMSSQL_SA_PASSWORD='{saPassword}'\n"
            + (database.Port != Docker.DefaultSqlPort ? $"MSSQL_PORT={database.Port}\n" : string.Empty));
        return result;
    }

    // Shows every step up front, then runs them one by one, each in its own color.
    private static async Task<bool> RunStepsAsync(IReadOnlyList<Step> steps)
    {
        var display = new StepDisplay();
        var finalLines = new List<string>();
        ProcessRunner.Result? failure = null;

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

                    var result = await step.Run();
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

    private const string NameRule = "Utilisez un espace de noms C# valide, par exemple Sample.Orders";

    // The hint ignores a trailing dot, so it doesn't vanish each time the user starts a new segment.
    private static string AskName(string outputDir) =>
        new InputPrompt(
                NameFromFolder(outputDir),
                validate: name => ValidName().IsMatch(name) ? null : NameRule,
                hint: name => ValidName().IsMatch(name.TrimEnd('.'))
                    ? $"Le projet API sera {Path.Combine("src", $"{name.TrimEnd('.')}.Api")}"
                    : NameRule)
            .Ask("nom", "Quel nom pour la solution ?");

    // A plain identifier (no dot), so T-SQL and Flyway never need [brackets] around it.
    // The suggestion is the solution name without its dots: Sample.Orders → SampleOrders.
    private static string AskDatabaseName(string solutionName) =>
        new InputPrompt(
                solutionName.Replace(".", string.Empty),
                validate: name => ValidDatabaseName().IsMatch(name) ? null : DatabaseNameRule,
                hint: name => ValidDatabaseName().IsMatch(name) ? "Utilisé dans la chaîne de connexion et par les scripts SQL" : DatabaseNameRule)
            .Ask("base", "Quel nom pour la base de données ?");

    private const string DatabaseNameRule = "Une lettre, puis lettres, chiffres ou _ (128 au plus), par exemple SampleOrders";

    private static readonly Regex ValidDatabaseNamePattern =
        new(@"^[A-Za-z][A-Za-z0-9_]{0,127}$", RegexOptions.Compiled);

    private static Regex ValidDatabaseName() => ValidDatabaseNamePattern;

    // SQL Server refuses to start with a weak sa password, so check its policy here rather than in the container logs.
    private static string AskSaPassword() =>
        new InputPrompt(
                string.Empty,
                validate: SaPasswordError,
                hint: password => SaPasswordError(password) ?? "Enregistré dans les secrets utilisateur de l'API et dans .env (non versionné)",
                summary: password => new string('•', password.Length),
                secret: true)
            .Ask("sql", "Quel mot de passe pour le compte sa de SQL Server ?");

    // At least 8 characters from 3 of the 4 groups: uppercase, lowercase, digits, symbols.
    // No single quote: .env wraps the value in single quotes so docker compose reads it literally.
    private static string? SaPasswordError(string password)
    {
        var groups = new[]
        {
            password.Any(char.IsUpper),
            password.Any(char.IsLower),
            password.Any(char.IsDigit),
            password.Any(c => !char.IsLetterOrDigit(c)),
        }.Count(present => present);

        return password.Length < 8 || groups < 3
            ? "Au moins 8 caractères, avec 3 types parmi majuscules, minuscules, chiffres et symboles"
            : password.Contains('\'') ? "Les apostrophes (') ne sont pas acceptées"
            : null;
    }

    private static bool AskYesNo(string badge, string question, bool yesFirst = true)
    {
        (string Label, bool Value)[] choices = yesFirst ? [("Oui", true), ("Non", false)] : [("Non", false), ("Oui", true)];
        return new SelectPrompt<(string Label, bool Value)>(choices, choice => choice.Label)
            .Ask(badge, question)
            .Value;
    }

    private static void ShowNextSteps(string name, string outputDir, IReadOnlyList<Templates.Module> modules, Database? database)
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

        if (database is not null)
        {
            ShowDatabaseConnection(database);
        }

        AnsiConsole.WriteLine();
    }

    // The fields as the VS Code mssql extension and SSMS ask for them. The password itself isn't printed.
    private static void ShowDatabaseConnection(Database database)
    {
        Ui.Badge("sql", "Connectez-vous à SQL Server (extension mssql de VS Code, SSMS)", Ui.Docker);
        if (!database.UseDocker)
        {
            Ui.Line("Démarrez d'abord SQL Server avec [#61AFEF]docker compose up -d[/]");
        }

        var grid = new Grid().AddColumn(new GridColumn().PadRight(2)).AddColumn();
        foreach (var (label, value) in new[]
        {
            ("Serveur", $"localhost,{database.Port}"),
            ("Authentification", "SQL Login"),
            ("Utilisateur", "sa"),
            ("Mot de passe", "celui saisi, aussi dans .env"),
            ("Base", database.Name),
            ("Certificat", "Trust server certificate (certificat auto-signé)"),
        })
        {
            grid.AddRow(new Markup($"[grey]{label}[/]"), new Markup($"[#61AFEF]{Markup.Escape(value)}[/]"));
        }

        AnsiConsole.Write(Ui.Indented(grid));
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
    // A leading "~" is the home folder, as in a shell: the prompts don't go through one to expand it.
    private static string? ToFullPath(string path)
    {
        try
        {
            path = path.Trim().Trim('"');
            if (path == "~" || path.StartsWith("~/") || path.StartsWith(@"~\"))
            {
                path = Path.Join(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), path[1..]);
            }

            var fullPath = Path.GetFullPath(path, Directory.GetCurrentDirectory());
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
