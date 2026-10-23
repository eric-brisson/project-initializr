namespace Starter.Init;

internal static class Templates
{
    // NextStep, when set, is shown in the final "suite" block if the module was selected.
    public sealed record Module(
        string Category,
        string Id,
        string Name,
        string Description,
        bool SelectedByDefault,
        string? NextStep = null);

    public sealed record Choice(string Name, string Description, string ShortName, IReadOnlyList<Module> Modules);

    // Add a line here when a new Starter template is published. Module ids match the template's "modules" choices.
    public static readonly IReadOnlyList<Choice> All =
    [
        new("minimal API", "Minimal API .NET, vertical avec tests", "project-initializr",
        [
            new("Documentation", "scalar", "Scalar", "référence d'API interactive", false,
                "Explorez l'API sur [#61AFEF]/scalar[/]"),
            new("Documentation", "swaggerui", "Swagger UI", "interface Swagger classique", false,
                "Ou avec Swagger UI sur [#61AFEF]/swagger[/]"),
        ]),
    ];
}
