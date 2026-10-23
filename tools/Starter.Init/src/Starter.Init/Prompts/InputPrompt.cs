using Spectre.Console;
using Spectre.Console.Rendering;

namespace Starter.Init.Prompts;

// Free text with a suggested value: Enter on an empty line keeps the suggestion, Tab copies it for editing.
// The hint line under the input updates as the user types. A secret input shows • instead of the characters.
internal sealed class InputPrompt(
    string suggestion,
    Func<string, string?> validate,
    Func<string, string?>? hint = null,
    Func<string, string>? summary = null,
    bool secret = false) : InlinePrompt<string>
{
    private string value = string.Empty;
    private string? error;

    private string Effective => value.Length == 0 ? suggestion : value;

    internal override string Result => Effective;

    internal override string Summary => summary?.Invoke(Effective) ?? Effective;

    internal override IRenderable Render()
    {
        var accent = Ui.Accent.ToMarkup();
        var input = value.Length == 0
            ? $"[{accent}]›[/] [{accent}]▌[/][grey]{Markup.Escape(suggestion)}[/]"
            : $"[{accent}]›[/] {Markup.Escape(secret ? new string('•', value.Length) : value)}[{accent}]▌[/]";

        var rows = new List<IRenderable> { new Markup(input) };
        if (error is not null)
        {
            rows.Add(new Markup($"[{Ui.Error.ToMarkup()}]{Markup.Escape(error)}[/]"));
        }
        else if (hint?.Invoke(Effective) is { } text)
        {
            rows.Add(new Markup($"[grey]{Markup.Escape(text)}[/]"));
        }

        return new Rows(rows);
    }

    internal override bool HandleKey(ConsoleKeyInfo key)
    {
        switch (key.Key)
        {
            case ConsoleKey.Enter:
                error = validate(Effective);
                return error is null;
            case ConsoleKey.Backspace:
                value = value.Length > 0 ? value[..^1] : value;
                break;
            case ConsoleKey.Tab when value.Length == 0:
                value = suggestion;
                break;
            case ConsoleKey.Escape:
                value = string.Empty;
                break;
            default:
                if (!char.IsControl(key.KeyChar))
                {
                    value += key.KeyChar;
                }

                break;
        }

        error = null;
        return false;
    }
}
