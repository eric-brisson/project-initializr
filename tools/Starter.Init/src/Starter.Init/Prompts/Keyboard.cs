namespace Starter.Init.Prompts;

// Where the prompts read keys from; tests swap it for scripted keys.
internal static class Keyboard
{
    public static Func<ConsoleKeyInfo> ReadKey { get; set; } = () => Console.ReadKey(intercept: true);

    // Prompts need a real keyboard and a real screen: not available when the input or the output
    // is redirected (scripts, pipelines, the "Run" window of some IDEs).
    public static bool IsAvailable { get; set; } = !Console.IsInputRedirected && !Console.IsOutputRedirected;
}
