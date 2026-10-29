using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Spectre.Console;
using Spectre.Console.Rendering;
using Starter.Init.Prompts;

namespace Starter.Init;

internal static partial class Program
{
    public static async Task<int> Main(string[] args)
    {
        // Windows consoles default to a legacy code page without symbols like ◆ and ✔.
        Console.OutputEncoding = Encoding.UTF8;

        // The prompts hide the cursor: bring it back if the user quits with Ctrl+C.
        Console.CancelKeyPress += (_, _) => AnsiConsole.Cursor.Show();

        ShowIntro();

        // starter-init feature [Name] adds a feature to the solution of the current folder.
        var featureCommand = args.Length > 0 && args[0] == FeatureCommand;

        // Arguments skip every prompt (for scripts): starter-init <name> [folder], or starter-init feature <Name>.
        // For a project, the first template and its default modules are used.
        var interactive = args.Length == (featureCommand ? 1 : 0);
        if (interactive && !Keyboard.IsAvailable)
        {
            Ui.Fail("Ce terminal ne permet pas les questions interactives.");
            Ui.Line("[grey]Lancez l'outil dans un vrai terminal (Windows Terminal, PowerShell), pas dans la fenêtre « Run » d'un IDE,[/]");
            Ui.Line("[grey]ou passez le nom en argument, par exemple : starter-init Sample.Orders ou starter-init feature Account[/]");
            return 1;
        }

        // Spectre turns live displays off when it sees a build server variable (TF_BUILD, GITHUB_ACTIONS,
        // JENKINS_URL, TEAMCITY_VERSION), even in a real terminal. With a real keyboard and screen, force them on.
        if (interactive)
        {
            AnsiConsole.Profile.Capabilities.Interactive = true;
        }

        if (featureCommand)
        {
            return await AddFeatureAsync(interactive ? null : args[1]);
        }

        if (interactive && AskAddFeature(FindStarterProject(Directory.GetCurrentDirectory())))
        {
            return await AddFeatureAsync(null);
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
                    module => module.SelectedByDefault,
                    module => module.Exclusive)
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
            Ui.Line("[grey]Depuis le dépôt des modèles, lancez : dotnet new install ./templates[/]");
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

        var initGit = interactive && await Git.CanInitAsync(outputDir!)
            && AskYesNo("git", "Initialiser un dépôt Git dans le dossier ?");

        // The first commit is the generated project as it was, to compare against or go back to.
        var commit = initGit && await Git.HasIdentityAsync();
        if (initGit)
        {
            Ui.Line(commit
                ? "[grey]Un premier commit sera créé sur main une fois les tests réussis[/]"
                : "[grey]Pas de premier commit : configurez user.name et user.email dans Git[/]");
        }

        var summary = Summary(template, outputDir!, name, modules, database, initGit, commit, keepGitignore, replaceGitignore);
        if (interactive && !AskYesNo("créer", "Tout est prêt. Générer le projet ?", details: summary))
        {
            return 0;
        }

        var solution = Path.Combine(outputDir!, $"{name}.sln");
        Step[] steps =
        [
            new("Génération du projet", "Projet généré", () => DotNetCli.RunAsync(
            [
                "new", template.ShortName, "--name", name, "--output", outputDir!,
                .. ModuleArguments(template, modules),
                .. (database is not null ? ["--databaseName", database.Name] : Array.Empty<string>()),
                .. (replaceGitignore ? ["--force"] : Array.Empty<string>()),
            ])),
            .. (initGit
                ? [new Step("Initialisation du dépôt Git", "Dépôt Git initialisé", () => Git.InitAsync(outputDir!))]
                : Array.Empty<Step>()),
            new("Restauration des paquets", "Paquets restaurés", () => DotNetCli.RunAsync("restore", solution)),
            .. (database is not null
                ? [new Step("Configuration de la base", "Base configurée",
                    () => ConfigureDatabaseAsync(outputDir!, name, database))]
                : Array.Empty<Step>()),
            new("Compilation de la solution", "Solution compilée", () => DotNetCli.RunAsync("build", solution, "--no-restore")),
            new("Exécution des tests", "Tests réussis", () => DotNetCli.RunAsync("test", solution, "--no-build")),
            .. (commit
                ? [new Step("Création du premier commit", "Premier commit créé",
                    () => Git.CommitAllAsync(outputDir!, InitialCommitMessage(template, modules)))]
                : Array.Empty<Step>()),

            // Last, so a Docker problem (image download, port) doesn't get in the way of the build and tests.
            .. (database is { UseDocker: true }
                ?
                [
                    new Step("Démarrage de SQL Server dans Docker", "SQL Server démarré dans Docker",
                        () => Docker.StartSqlServerAsync(outputDir!)),
                    new Step($"Création de la database {database.Name}", $"Database {database.Name} créée",
                        () => Docker.CreateDatabaseAsync(outputDir!, database.Name, database.SaPassword)),
                    new Step("Application des migrations Flyway", "Migrations Flyway appliquées",
                        () => Docker.MigrateAsync(outputDir!)),
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

    private sealed record Step(string Running, string Done, Func<Task<ProcessRunner.Result>> Run);

    // UseDocker is false when Docker isn't available: the project is still configured, the container just isn't started.
    private sealed record Database(string Name, string SaPassword, int Port, bool UseDocker);

    // e.g. "Initial commit (project-initializr: scalar, database)", so the log tells how the project was generated.
    private static string InitialCommitMessage(Templates.Choice template, IReadOnlyList<Templates.Module> modules) =>
        modules.Count == 0
            ? $"Initial commit ({template.ShortName})"
            : $"Initial commit ({template.ShortName}: {string.Join(", ", modules.Select(module => module.Id))})";

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

        var useDocker = await Docker.IsAvailableAsync() || await AskToStartDockerAsync();
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

    // Docker Desktop is often just not started yet: offer to start it (or retry) rather than going on without SQL Server.
    // Returns false when the user chooses to continue without Docker.
    private static async Task<bool> AskToStartDockerAsync()
    {
        const string Start = "Démarrer Docker Desktop", Retry = "Réessayer", Skip = "Continuer sans Docker";
        string[] choices = Docker.CanStartDesktop ? [Start, Retry, Skip] : [Retry, Skip];
        var details = Docker.CanStartDesktop
            ? "Docker Desktop sera lancé, puis l'outil attendra qu'il soit prêt"
            : "Lancez Docker, puis choisissez Réessayer";

        while (true)
        {
            var choice = new SelectPrompt<string>(choices, choice => choice, choice => choice switch
                {
                    Start => details,
                    Retry => "une fois Docker lancé",
                    _ => "le projet sera configuré, sans démarrer SQL Server",
                })
                .Ask("docker", $"[{Ui.Warning.ToMarkup()}]Docker n'est pas disponible.[/] Que voulez-vous faire ?");

            if (choice == Skip)
            {
                return false;
            }

            if (choice == Start && !await Docker.StartDesktopAsync())
            {
                Ui.Line($"[{Ui.Warning.ToMarkup()}]Impossible de lancer Docker Desktop : lancez-le vous-même, puis choisissez Réessayer[/]");
                continue;
            }

            // Docker Desktop takes a while to start its engine after the app opens.
            var timeout = choice == Start ? TimeSpan.FromMinutes(2) : TimeSpan.FromSeconds(5);
            var available = await AnsiConsole.Status()
                .Spinner(Spinner.Known.Dots)
                .StartAsync($"{Ui.Indent}[grey]En attente de Docker...[/]", _ => Docker.WaitUntilAvailableAsync(timeout));
            if (available)
            {
                return true;
            }

            Ui.Line($"[{Ui.Warning.ToMarkup()}]Docker ne répond toujours pas[/]");
        }
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

    // Shows every step up front, then runs them one by one. All steps share the accent color, so the green
    // "Projet initialisé !" after them stands out.
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
                    var color = Ui.Accent.ToMarkup();
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

    private static bool AskYesNo(string badge, string question, bool yesFirst = true, IRenderable? details = null)
    {
        (string Label, bool Value)[] choices = yesFirst ? [("Oui", true), ("Non", false)] : [("Non", false), ("Oui", true)];
        return new SelectPrompt<(string Label, bool Value)>(choices, choice => choice.Label)
            .Ask(badge, question, details)
            .Value;
    }

    // Every choice in one place before generating, including what the tool decided itself (port, Docker, commit).
    // Lines that don't apply are left out, and the password is never shown.
    private static IRenderable Summary(
        Templates.Choice template,
        string outputDir,
        string name,
        IReadOnlyList<Templates.Module> modules,
        Database? database,
        bool initGit,
        bool commit,
        bool keepGitignore,
        bool replaceGitignore)
    {
        var rows = new List<(string, string)>
        {
            ("Modèle", Markup.Escape(template.Name)),
            ("Dossier", Markup.Escape(outputDir)),
            ("Solution", Markup.Escape($"{name} → {Path.Combine("src", $"{name}.Api")}")),
        };

        if (template.Modules.Count > 0)
        {
            rows.Add(("Modules", modules.Count == 0 ? "aucun" : Markup.Escape(string.Join(", ", modules.Select(module => module.Name)))));
        }

        if (database is not null)
        {
            var docker = database.UseDocker
                ? "démarrée dans Docker"
                : $"[{Ui.Warning.ToMarkup()}]Docker indisponible, SQL Server ne sera pas démarré[/]";
            rows.Add(("Base de données", $"{Markup.Escape(database.Name)}, port {database.Port}, {docker}"));
        }

        if (initGit)
        {
            rows.Add(("Git", commit ? "dépôt sur main + premier commit" : "dépôt sur main, sans premier commit"));
        }

        if (keepGitignore || replaceGitignore)
        {
            rows.Add((".gitignore", keepGitignore ? "conservé" : "remplacé par celui du modèle"));
        }

        return Ui.Details(rows);
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
            Ui.Line("Démarrez d'abord SQL Server avec [#61AFEF]docker compose up -d[/], créez la base,");
            Ui.Line("puis appliquez les migrations avec [#61AFEF]docker compose run --rm flyway[/]");
        }

        AnsiConsole.Write(Ui.Indented(Ui.Details(new[]
        {
            ("Serveur", $"localhost,{database.Port}"),
            ("Authentification", "SQL Login"),
            ("Utilisateur", "sa"),
            ("Mot de passe", "celui saisi, aussi dans .env"),
            ("Base de données", database.Name),
            ("Certificat", "Trust server certificate (certificat auto-signé)"),
        }.Select(row => (row.Item1, Markup.Escape(row.Item2))))));
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
