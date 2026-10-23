using Spectre.Console;
using Spectre.Console.Rendering;

namespace Starter.Init;

// Astro-style layout: a column of colored badges on the left, everything else aligned after it.
internal static class Ui
{
    public const int BadgeWidth = 10;

    public static readonly string Indent = new(' ', BadgeWidth + 2);

    public static readonly Color Accent = Color.FromHex("#8B9BFA");
    public static readonly Color Success = Color.FromHex("#98C379");
    public static readonly Color Info = Color.FromHex("#61AFEF");
    public static readonly Color Error = Color.FromHex("#E06C75");
    public static readonly Color Warning = Color.FromHex("#E5C07B");
    public static readonly Color Docker = Color.FromHex("#56B6C2");

    // A blank line, then the badge right-aligned in its column, followed by the text.
    public static void Badge(string label, string text, Color? color = null)
    {
        var padding = new string(' ', Math.Max(0, BadgeWidth - label.Length - 2));
        AnsiConsole.WriteLine();
        AnsiConsole.MarkupLine($"{padding}[black on {(color ?? Accent).ToMarkup()}] {Markup.Escape(label)} [/]  {text}");
    }

    // Indented through a Padder (not leading spaces), so long lines like paths wrap under the text column.
    public static void Line(string markup) => AnsiConsole.Write(Indented(new Markup(markup)));

    public static void Answer(string text) => Line($"[grey]{Markup.Escape(text)}[/]");

    public static void Fail(string text) => Badge("erreur", $"[{Error.ToMarkup()}]{Markup.Escape(text)}[/]", Error);

    // Grey labels and blue values in two columns. Values are markup: escape plain text before passing it.
    public static IRenderable Details(IEnumerable<(string Label, string Value)> rows)
    {
        var grid = new Grid().AddColumn(new GridColumn().PadRight(2)).AddColumn();
        foreach (var (label, value) in rows)
        {
            grid.AddRow(new Markup($"[grey]{Markup.Escape(label)}[/]"), new Markup($"[{Info.ToMarkup()}]{value}[/]"));
        }

        return grid;
    }

    public static IRenderable Indented(IRenderable content) => new Padder(content, new Padding(BadgeWidth + 2, 0, 0, 0));
}
