namespace Starter.Init.Prompts;

// Where the prompts read keys from; tests swap it for scripted keys.
internal static class Keyboard
{
    public static Func<ConsoleKeyInfo> ReadKey { get; set; } = () => Console.ReadKey(intercept: true);

    // Prompts need a real keyboard: not available when the input is redirected (scripts, pipelines).
    public static bool IsAvailable { get; set; } = !Console.IsInputRedirected;
}
