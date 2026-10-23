using Spectre.Console;
using Spectre.Console.Rendering;

namespace Starter.Init.Prompts;

// Several choices grouped by category: ◼ checked, ◻ unchecked, Space toggles, Enter validates.
internal sealed class MultiSelectPrompt<T> : InlinePrompt<IReadOnlyList<T>>
{
    private static readonly Color CheckedColor = Color.FromHex("#A6E22E");

    private readonly IReadOnlyList<T> items;
    private readonly Func<T, string> category;
    private readonly Func<T, string> name;
    private readonly Func<T, string?> description;
    private readonly HashSet<int> checkedIndexes;
    private int index;

    public MultiSelectPrompt(
        IReadOnlyList<T> items,
        Func<T, string> category,
        Func<T, string> name,
        Func<T, string?> description,
        Func<T, bool> checkedByDefault)
    {
        // Keep items of the same category together, in their order of appearance.
        this.items = items.GroupBy(category).SelectMany(group => group).ToList();
        this.category = category;
        this.name = name;
        this.description = description;
        checkedIndexes = [.. Enumerable.Range(0, this.items.Count).Where(i => checkedByDefault(this.items[i]))];
    }

    internal override IReadOnlyList<T> Result =>
        [.. items.Where((_, i) => checkedIndexes.Contains(i))];

    internal override string Summary =>
        Result.Count == 0 ? "Aucun module" : string.Join(", ", Result.Select(name));

    internal override IRenderable Render()
    {
        var rows = new List<IRenderable>();
        string? currentCategory = null;

        for (var i = 0; i < items.Count; i++)
        {
            var item = items[i];
            if (category(item) != currentCategory)
            {
                currentCategory = category(item);
                rows.Add(new Markup($"[bold]{Markup.Escape(currentCategory)}[/]"));
            }

            var box = checkedIndexes.Contains(i) ? $"[{CheckedColor.ToMarkup()}]◼[/]" : "[grey]◻[/]";
            rows.Add(new Markup(i == index
                ? $"  {box} {Markup.Escape(name(item))}{Describe(item)}"
                : $"  {box} [grey]{Markup.Escape(name(item))}[/]"));
        }

        rows.Add(new Markup("[grey](flèches pour naviguer, Espace pour cocher, Entrée pour valider)[/]"));
        return new Rows(rows);
    }

    internal override bool HandleKey(ConsoleKeyInfo key)
    {
        switch (key.Key)
        {
            case ConsoleKey.UpArrow:
                index = (index - 1 + items.Count) % items.Count;
                return false;
            case ConsoleKey.DownArrow:
                index = (index + 1) % items.Count;
                return false;
            case ConsoleKey.Spacebar:
                if (!checkedIndexes.Remove(index))
                {
                    checkedIndexes.Add(index);
                }

                return false;
            case ConsoleKey.Enter:
                return true;
            default:
                return false;
        }
    }

    private string Describe(T item) =>
        description(item) is { } text ? $" [grey]({Markup.Escape(text)})[/]" : string.Empty;
}
