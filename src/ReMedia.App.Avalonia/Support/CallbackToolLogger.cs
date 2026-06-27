namespace ReMedia.App.Avalonia.Support;

using ReMedia.Core.Diagnostics;

/// <summary>Bridges <see cref="IToolLogger"/> to a UI log sink via a callback.</summary>
public sealed class CallbackToolLogger : IToolLogger
{
    private readonly Action<string> _append;

    public CallbackToolLogger(Action<string> append)
    {
        _append = append;
    }

    public void LogMessage(string message)
    {
        _append(message);
    }
}
