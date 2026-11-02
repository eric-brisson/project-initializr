namespace Starter.Init;

// The questions of the tool. Each one is drawn under its badge, aligned with the question; once it's answered, the
// choices disappear and only the answer stays, in grey. The lists are drawn here (Spectre's own selection prompts
// can't be indented and always use a > marker); free text uses Spectre's TextPrompt.
internal static class Ask
{
    private static readonly Color CheckedColor = Color.FromHex("#A6E22E");

    // One choice among several, picked with the arrows: ● the current one, with its description, ○ the others.
    // details, when set, is drawn between the question and the choices (e.g. a summary to review).
    public static T Choice<T>(
        string badge,
        string question,
        IEnumerable<T> choices,
        Func<T, string> name,
        Func<T, string?>? description = null,
        IRenderable? details = null)
    {
        Question(badge, question, details);

        var items = choices.ToList();
        var index = 0;
        ReadKeys(
            () => List(
                items.Select((item, i) => i == index
                    ? $"[{Ui.Accent.ToMarkup()}]●[/] {Markup.Escape(name(item))}{Describe(description?.Invoke(item))}"
                    : $"[grey]○ {Markup.Escape(name(item))}[/]"),
                "(flèches pour choisir, Entrée pour valider)"),
            key =>
            {
                index = Move(key, index, items.Count);
                return key.Key == ConsoleKey.Enter;
            });

        Ui.Answer(name(items[index]));
        return items[index];
    }

    public static bool YesNo(string badge, string question, bool yesFirst = true, IRenderable? details = null) =>
        Choice<bool>(badge, question, yesFirst ? [true, false] : [false, true], yes => yes ? "Oui" : "Non", details: details);

    // Several choices: ◼ checked, ◻ unchecked, Space toggles, Enter validates. None is a valid answer.
    public static List<T> Choices<T>(
        string badge,
        string question,
        IEnumerable<T> choices,
        Func<T, string> name,
        Func<T, string?> description,
        Func<T, bool> checkedByDefault)
    {
        Question(badge, question);

        var items = choices.ToList();
        var isChecked = items.Select(checkedByDefault).ToArray();
        var index = 0;
        ReadKeys(
            () => List(
                items.Select((item, i) =>
                {
                    var box = isChecked[i] ? $"[{CheckedColor.ToMarkup()}]◼[/]" : "[grey]◻[/]";
                    return i == index
                        ? $"{box} {Markup.Escape(name(item))}{Describe(description(item))}"
                        : $"{box} [grey]{Markup.Escape(name(item))}[/]";
                }),
                "(flèches pour naviguer, Espace pour cocher, Entrée pour valider)"),
            key =>
            {
                if (key.Key == ConsoleKey.Spacebar)
                {
                    isChecked[index] = !isChecked[index];
                }

                index = Move(key, index, items.Count);
                return key.Key == ConsoleKey.Enter;
            });

        var answer = items.Where((_, i) => isChecked[i]).ToList();
        Ui.Answer(answer.Count == 0 ? "Aucun" : string.Join(", ", answer.Select(name)));
        return answer;
    }

    // Free text. Enter on an empty line keeps the suggestion, if any. validate returns the error to show, or null.
    // help is a grey line shown before the input (the rule to follow); answer, a grey line shown after it
    // (e.g. what the value leads to). A secret input shows • instead of the characters.
    public static string Text(
        string badge,
        string question,
        string suggestion,
        Func<string, string?> validate,
        string? help = null,
        Func<string, string>? answer = null,
        bool secret = false)
    {
        Question(badge, question);
        if (help is not null)
        {
            Ui.Line($"[grey]{Markup.Escape(help)}[/]");
        }

        // Spectre drops the leading spaces of a prompt: the indentation is written just before it. The answer is
        // validated here rather than by Spectre, so the error and the next attempt stay aligned too.
        while (true)
        {
            AnsiConsole.Write(Ui.Indent);
            var prompt = new TextPrompt<string>($"[{Ui.Accent.ToMarkup()}]›[/]").AllowEmpty();
            if (suggestion.Length > 0)
            {
                prompt.DefaultValue(suggestion).DefaultValueStyle(new Style(Color.Grey));
            }

            if (secret)
            {
                prompt.Secret('•');
            }

            var value = AnsiConsole.Prompt(prompt);
            if (validate(value) is { } error)
            {
                Ui.Line($"[{Ui.Error.ToMarkup()}]{Markup.Escape(error)}[/]");
                continue;
            }

            if (answer is not null)
            {
                Ui.Answer(answer(value));
            }

            return value;
        }
    }

    private static void Question(string badge, string question, IRenderable? details = null)
    {
        Ui.Badge(badge, question);
        if (details is not null)
        {
            AnsiConsole.Write(Ui.Indented(details));
        }
    }

    // Draws the list under the question and redraws it after each key, until handleKey returns true. The list then
    // disappears: the caller writes the answer in its place.
    private static void ReadKeys(Func<IRenderable> render, Func<ConsoleKeyInfo, bool> handleKey)
    {
        AnsiConsole.Live(Ui.Indented(render()))
            .AutoClear(true)
            .Start(context =>
            {
                // Live only draws on refresh: show the list before waiting for the first key.
                context.Refresh();
                while (!handleKey(Console.ReadKey(intercept: true)))
                {
                    context.UpdateTarget(Ui.Indented(render()));
                }
            });
    }

    private static IRenderable List(IEnumerable<string> rows, string instructions) =>
        new Rows([.. rows.Select(row => new Markup(row)), new Markup($"[grey]{instructions}[/]")]);

    // Up and down arrows, going round at both ends.
    private static int Move(ConsoleKeyInfo key, int index, int count) => key.Key switch
    {
        ConsoleKey.UpArrow => (index - 1 + count) % count,
        ConsoleKey.DownArrow => (index + 1) % count,
        _ => index,
    };

    private static string Describe(string? description) =>
        description is null ? string.Empty : $" [grey]({Markup.Escape(description)})[/]";
}
