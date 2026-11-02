namespace Starter.Init;

internal static class Templates
{
    // One question per category, asked in the order the modules are declared. A category with OneChoice offers its
    // modules plus None (the first choice) as one choice; otherwise several modules can be checked, or answered
    // yes or no when the category has only one. Badge is the label of the question (8 characters at most).
    public sealed record Category(
        string Badge,
        string Question,
        bool OneChoice = false,
        string? None = null,
        string? NoneDescription = null);

    // NextStep, when set, is shown in the final "suite" block if the module was selected.
    public sealed record Module(
        Category Category,
        string Id,
        string Name,
        string Description,
        bool SelectedByDefault,
        string? NextStep = null);

    public sealed record Choice(string Name, string Description, string ShortName, IReadOnlyList<Module> Modules);

    // The item template that adds a feature to a generated solution (starter-init feature).
    public const string FeatureShortName = "starter-feature";

    // Selecting this module asks for the sa password, stored in the API's user secrets and in .env.
    public const string DatabaseId = "database";

    // Selecting this module starts the Aspire dashboard of compose.yaml once the project is generated.
    public const string TelemetryId = "telemetry";

    private static readonly Category Documentation =
        new("doc", "Quelle documentation d'API voulez-vous ?");

    private static readonly Category Data =
        new("données", "Où stocker les données ?", OneChoice: true,
            None: "Aucune base de données", NoneDescription: "un store en mémoire par feature, sans EF Core");

    private static readonly Category Observability =
        new("traces", "Ajouter la télémétrie locale ?");

    // Add a line here when a new Starter template is published. Module ids match the template's "modules" choices.
    public static readonly IReadOnlyList<Choice> All =
    [
        new("minimal API", "Minimal API .NET, vertical avec tests", "project-initializr",
        [
            new(Documentation, "scalar", "Scalar", "référence d'API interactive", false,
                "Explorez l'API sur [#61AFEF]/scalar[/]"),
            new(Documentation, "swaggerui", "Swagger UI", "interface Swagger classique", false,
                "Ou avec Swagger UI sur [#61AFEF]/swagger[/]"),
            new(Data, "inmemory", "SQLite en mémoire", "base relationnelle sans Docker, vidée à chaque démarrage", false,
                "Les données ajoutées sont perdues à chaque redémarrage de l'API (SQLite en mémoire)"),
            // No NextStep: Program shows the connection details instead.
            new(Data, DatabaseId, "SQL Server", "conteneur Docker avec la base du projet, migrations Flyway", false),
            new(Observability, TelemetryId, "Télémétrie locale",
                "traces, logs et métriques dans le tableau de bord Aspire (Docker), pendant le développement", false,
                "Suivez les requêtes sur [#61AFEF]http://localhost:18888[/] : le tableau de bord démarre aussi avec l'API en debug"),
        ]),
    ];
}
