using System.Text.Json;
using System.Text.RegularExpressions;
using Spectre.Console;
using Starter.Init.Prompts;

namespace Starter.Init;

// starter-init feature: adds a CRUD feature (the starter-feature item template) to a generated solution.
// The tool only creates files: the message at the end tells what to wire, and where.
internal static partial class Program
{
    private const string FeatureCommand = "feature";

    // Data is "none", "sqlite" or "sqlserver", as the data module of the project.
    private sealed record StarterProject(string Root, string Solution, string Data);

    // Inside a generated solution, adding a feature is the likelier choice: it comes first.
    private static bool AskAddFeature(StarterProject? project)
    {
        (string Label, bool Feature) create = ("Créer un projet", false);
        (string Label, bool Feature) feature = ("Ajouter une feature", true);

        return new SelectPrompt<(string Label, bool Feature)>(
                project is null ? [create, feature] : [feature, create],
                choice => choice.Label,
                choice => choice.Feature
                    ? project is null ? "à une solution existante" : $"à {project.Solution}"
                    : "nouvelle solution")
            .Ask("action", "Que voulez-vous faire ?")
            .Feature;
    }

    // name is null in interactive mode.
    private static async Task<int> AddFeatureAsync(string? name)
    {
        var project = FindStarterProject(Directory.GetCurrentDirectory());
        if (project is null)
        {
            Ui.Fail("Aucune solution Starter dans ce dossier ou ses parents.");
            Ui.Line("[grey]Lancez la commande depuis le dossier d'un projet généré (celui qui contient .starter.json).[/]");
            return 1;
        }

        var interactive = name is null;
        if (interactive)
        {
            Ui.Badge("solution", $"{Markup.Escape(project.Solution)} [grey]({DataDescription(project.Data)})[/]");
        }

        name ??= AskFeatureName();
        if (!ValidFeatureName().IsMatch(name))
        {
            Ui.Fail($"'{name}' n'est pas un nom de feature valide. {FeatureNameRule}");
            return 1;
        }

        var plural = interactive ? AskPlural(name) : Pluralize(name);
        var folder = Path.Combine(project.Root, "src", $"{project.Solution}.Api", "Features", plural);
        if (Directory.Exists(folder))
        {
            Ui.Fail($"La feature {plural} existe déjà ({folder}).");
            return 1;
        }

        if (!await DotNetCli.IsTemplateInstalledAsync(Templates.FeatureShortName))
        {
            Ui.Fail($"Le modèle '{Templates.FeatureShortName}' n'est pas installé.");
            Ui.Line("[grey]Depuis le dépôt des modèles, lancez : dotnet new install ./templates[/]");
            return 1;
        }

        if (interactive && !AskYesNo("créer", "Tout est prêt. Générer la feature ?", details: FeatureSummary(project, name, plural)))
        {
            return 0;
        }

        Step[] steps =
        [
            new("Génération de la feature", "Feature générée", () => DotNetCli.RunAsync(
            [
                "new", Templates.FeatureShortName, "--name", name, "--plural", plural,
                "--solution", project.Solution, "--storage", project.Data == "none" ? "memory" : "efcore",
                "--output", project.Root,
            ])),

            // The tests aren't run: they fail until the feature is wired.
            new("Compilation de la solution", "Solution compilée",
                () => DotNetCli.RunAsync("build", Path.Combine(project.Root, $"{project.Solution}.sln"))),
        ];

        Ui.Badge("étapes", $"Création de la feature {plural}");
        if (!await RunStepsAsync(steps))
        {
            return 1;
        }

        ShowFeatureWiring(project, name, plural);
        return 0;
    }

    private const string FeatureNameRule = "Un nom en PascalCase, au singulier, par exemple Account ou UserProfile";

    private static string AskFeatureName() =>
        new InputPrompt(
                "Account",
                validate: name => ValidFeatureName().IsMatch(name) ? null : FeatureNameRule,
                hint: name => ValidFeatureName().IsMatch(name)
                    ? $"Les classes seront {name}, Create{name}, Get{name}…"
                    : FeatureNameRule)
            .Ask("feature", "Quel nom pour la feature (au singulier) ?");

    private static string AskPlural(string name) =>
        new InputPrompt(
                Pluralize(name),
                validate: plural => !ValidFeatureName().IsMatch(plural) ? "Un nom en PascalCase, par exemple Accounts"
                    : plural == name ? "Le pluriel doit être différent du nom"
                    : null,
                hint: plural => ValidFeatureName().IsMatch(plural)
                    ? $"Dossier Features/{plural}, route /api/v1/{Kebab(plural)}"
                    : "Un nom en PascalCase, par exemple Accounts")
            .Ask("pluriel", "Quel pluriel ?");

    private static Spectre.Console.Rendering.IRenderable FeatureSummary(StarterProject project, string name, string plural) =>
        Ui.Details(new[]
        {
            ("Solution", project.Solution),
            ("Feature", $"{name} → {Path.Combine("src", $"{project.Solution}.Api", "Features", plural)}"),
            ("Routes", $"/api/v1/{Kebab(plural)} (liste, lecture, création, modification, suppression)"),
            ("Stockage", DataDescription(project.Data)),
        }.Select(row => (row.Item1, Markup.Escape(row.Item2))));

    // The lines to add by hand, with the files they go in. Nothing to add to AppDbContext:
    // it picks up the <Name>Configuration class by itself.
    private static void ShowFeatureWiring(StarterProject project, string name, string plural)
    {
        var api = Path.Combine("src", $"{project.Solution}.Api");
        var featureNamespace = $"{project.Solution}.Api.Features.{plural}";
        var step = 0;

        void Instruction(string text, params string[] code)
        {
            Ui.Line($"[grey]{++step}.[/] {text}");
            foreach (var line in code)
            {
                Ui.Line($"   [#61AFEF]{Markup.Escape(line)}[/]");
            }
        }

        Ui.Line($"[{Ui.Accent.ToMarkup()}]✔[/] [{Ui.Success.ToMarkup()}]Feature {plural} créée ![/]");
        Ui.Badge("suite", "Il reste à la brancher :", Ui.Info);

        Instruction(
            $"Dans [#61AFEF]{Markup.Escape(Path.Combine(api, "Common", "EndpointRegistration.cs"))}[/], mappez les routes :",
            $"using {featureNamespace};",
            $"v1.Map{plural}Endpoints();");
        Instruction(
            $"Dans [#61AFEF]{Markup.Escape(Path.Combine(api, "Common", "ServiceRegistration.cs"))}[/], enregistrez les services :",
            $"using {featureNamespace};",
            $"services.Add{plural}Services();");

        if (project.Data == "sqlserver")
        {
            var script = Path.Combine("db", "migrations", $"V{NextMigrationVersion(project.Root)}__create_{Kebab(plural).Replace('-', '_')}.sql");
            Instruction(
                $"Créez la table {plural} dans un nouveau script Flyway, par exemple [#61AFEF]{Markup.Escape(script)}[/], puis appliquez-le :",
                "docker compose run --rm flyway");
        }

        Instruction($"Complétez les TODO : les propriétés de {name}, de {name}Dto et des requêtes de création et de modification.");

        Ui.Line(project.Data == "sqlserver"
            ? $"[grey]Les tests d'intégration de {plural} échouent tant que la feature n'est pas branchée et la table créée.[/]"
            : $"[grey]Les tests d'intégration de {plural} échouent tant que la feature n'est pas branchée.[/]");
        AnsiConsole.WriteLine();
    }

    private static string DataDescription(string data) => data switch
    {
        "sqlserver" => "EF Core, SQL Server",
        "sqlite" => "EF Core, SQLite en mémoire",
        _ => "en mémoire, sans base de données",
    };

    // The folder of the current one (or a parent) that holds .starter.json, written by the project template.
    // Older solutions without it are recognized by <name>.sln next to src/<name>.Api, and their data module
    // by the EF Core provider the API references.
    private static StarterProject? FindStarterProject(string directory)
    {
        for (var current = new DirectoryInfo(directory); current is not null; current = current.Parent)
        {
            var settings = Path.Combine(current.FullName, ".starter.json");
            if (File.Exists(settings))
            {
                return ReadStarterProject(current.FullName, settings);
            }

            var solution = current.GetFiles("*.sln").Select(file => Path.GetFileNameWithoutExtension(file.Name)).FirstOrDefault();
            var api = solution is null ? null : Path.Combine(current.FullName, "src", $"{solution}.Api", $"{solution}.Api.csproj");
            if (api is not null && File.Exists(api))
            {
                var csproj = File.ReadAllText(api);
                var data = csproj.Contains("EntityFrameworkCore.SqlServer") ? "sqlserver"
                    : csproj.Contains("EntityFrameworkCore.Sqlite") ? "sqlite"
                    : "none";
                return new StarterProject(current.FullName, solution!, data);
            }
        }

        return null;
    }

    // The file has comments. A repeated property (only in the template repo itself) keeps its last value.
    private static StarterProject? ReadStarterProject(string root, string path)
    {
        try
        {
            using var document = JsonDocument.Parse(
                File.ReadAllText(path),
                new JsonDocumentOptions { CommentHandling = JsonCommentHandling.Skip, AllowTrailingCommas = true });
            var properties = document.RootElement.EnumerateObject()
                .GroupBy(property => property.Name)
                .ToDictionary(group => group.Key, group => group.Last().Value.GetString());

            return properties.GetValueOrDefault("solution") is { } solution && ValidName().IsMatch(solution)
                ? new StarterProject(root, solution, properties.GetValueOrDefault("data") ?? "none")
                : null;
        }
        catch (JsonException)
        {
            return null;
        }
    }

    // The highest V<n>__ of db/migrations, plus one.
    private static int NextMigrationVersion(string root)
    {
        var folder = Path.Combine(root, "db", "migrations");
        var versions = Directory.Exists(folder)
            ? Directory.EnumerateFiles(folder, "V*__*.sql")
                .Select(file => MigrationVersion().Match(Path.GetFileName(file)))
                .Where(match => match.Success)
                .Select(match => int.Parse(match.Groups[1].Value))
            : [];

        return versions.DefaultIfEmpty(0).Max() + 1;
    }

    // Common English rules, good enough for a suggestion the user can change: Category → Categories, Box → Boxes.
    private static string Pluralize(string name) =>
        Regex.IsMatch(name, "[^aeiou]y$") ? name[..^1] + "ies"
        : Regex.IsMatch(name, "(s|x|z|ch|sh)$") ? name + "es"
        : name + "s";

    // UserProfiles → user-profiles, as the template does for the route.
    private static string Kebab(string name) => Regex.Replace(name, "([a-z0-9])([A-Z])", "$1-$2").ToLowerInvariant();

    private static readonly Regex ValidFeatureNamePattern = new("^[A-Z][A-Za-z0-9]*$", RegexOptions.Compiled);

    private static Regex ValidFeatureName() => ValidFeatureNamePattern;

    private static readonly Regex MigrationVersionPattern = new(@"^V(\d+)__", RegexOptions.Compiled);

    private static Regex MigrationVersion() => MigrationVersionPattern;
}
