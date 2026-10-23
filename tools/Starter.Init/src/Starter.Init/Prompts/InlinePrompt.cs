using Spectre.Console;
using Spectre.Console.Rendering;

namespace Starter.Init.Prompts;

// A question drawn under its badge and driven by the keyboard. Once answered,
// the interactive part disappears and only the answer stays, in grey.
internal abstract class InlinePrompt<T>
{
    // details, when set, is drawn between the question and the choices (e.g. a summary to review).
    public T Ask(string badge, string question, IRenderable? details = null)
    {
        Ui.Badge(badge, question);
        if (details is not null)
        {
            AnsiConsole.Write(Ui.Indented(details));
        }

        AnsiConsole.Live(Ui.Indented(Render()))
            .AutoClear(true)
            .Start(context =>
            {
                // Live only draws on refresh: show the choices before waiting for the first key.
                context.Refresh();

                while (!HandleKey(Keyboard.ReadKey()))
                {
                    context.UpdateTarget(Ui.Indented(Render()));
                }
            });

        Ui.Answer(Summary);
        return Result;
    }

    internal abstract T Result { get; }

    // The grey line left once the question is answered.
    internal abstract string Summary { get; }

    internal abstract IRenderable Render();

    // Returns true once the answer is accepted.
    internal abstract bool HandleKey(ConsoleKeyInfo key);
}
