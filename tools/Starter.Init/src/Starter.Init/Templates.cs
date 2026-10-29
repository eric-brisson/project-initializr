namespace Starter.Init;

internal static class Templates
{
    // NextStep, when set, is shown in the final "suite" block if the module was selected.
    // At most one Exclusive module per category can be selected: checking one unchecks the others.
    public sealed record Module(
        string Category,
        string Id,
        string Name,
        string Description,
        bool SelectedByDefault,
        string? NextStep = null,
        bool Exclusive = false);

    public sealed record Choice(string Name, string Description, string ShortName, IReadOnlyList<Module> Modules);

    // The item template that adds a feature to a generated solution (starter-init feature).
    public const string FeatureShortName = "starter-feature";

    // Selecting this module asks for the sa password, stored in the API's user secrets and in .env.
    public const string DatabaseId = "database";

    // Add a line here when a new Starter template is published. Module ids match the template's "modules" choices.
    public static readonly IReadOnlyList<Choice> All =
    [
        new("minimal API", "Minimal API .NET, vertical avec tests", "project-initializr",
        [
            new("Documentation", "scalar", "Scalar", "référence d'API interactive", false,
                "Explorez l'API sur [#61AFEF]/scalar[/]"),
            new("Documentation", "swaggerui", "Swagger UI", "interface Swagger classique", false,
                "Ou avec Swagger UI sur [#61AFEF]/swagger[/]"),
            new("Données", "inmemory", "SQLite en mémoire", "base relationnelle sans Docker, vidée à chaque démarrage", false,
                "Les données ajoutées sont perdues à chaque redémarrage de l'API (SQLite en mémoire)", Exclusive: true),
            // No NextStep: Program shows the connection details instead.
            new("Données", DatabaseId, "SQL Server", "conteneur Docker avec la base du projet, migrations Flyway", false,
                Exclusive: true),
        ]),
    ];
}
