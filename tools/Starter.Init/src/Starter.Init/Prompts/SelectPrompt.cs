using Spectre.Console;
using Spectre.Console.Rendering;

namespace Starter.Init.Prompts;

// One choice among several: ● current, ○ others, with the description of the current one.
internal sealed class SelectPrompt<T>(
    IReadOnlyList<T> choices,
    Func<T, string> name,
    Func<T, string?>? description = null) : InlinePrompt<T>
{
    private int index;

    internal override T Result => choices[index];

    internal override string Summary => name(choices[index]);

    internal override IRenderable Render()
    {
        var rows = choices
            .Select((choice, i) => (IRenderable)new Markup(i == index
                ? $"[{Ui.Accent.ToMarkup()}]●[/] {Markup.Escape(name(choice))}{Describe(choice)}"
                : $"[grey]○ {Markup.Escape(name(choice))}[/]"))
            .ToList();

        rows.Add(new Markup("[grey](flèches pour choisir, Entrée pour valider)[/]"));
        return new Rows(rows);
    }

    internal override bool HandleKey(ConsoleKeyInfo key)
    {
        switch (key.Key)
        {
            case ConsoleKey.UpArrow:
                index = (index - 1 + choices.Count) % choices.Count;
                return false;
            case ConsoleKey.DownArrow:
                index = (index + 1) % choices.Count;
                return false;
            case ConsoleKey.Enter:
                return true;
            default:
                return false;
        }
    }

    private string Describe(T choice) =>
        description?.Invoke(choice) is { } text ? $" [grey]({Markup.Escape(text)})[/]" : string.Empty;
}
