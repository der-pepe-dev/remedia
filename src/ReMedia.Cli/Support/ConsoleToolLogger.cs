namespace ReMedia.Cli.Support;

using ReMedia.Core.Diagnostics;

internal sealed class ConsoleToolLogger : IToolLogger
{
    public void LogMessage(string message)
    {
        Console.WriteLine(message);
    }
}
