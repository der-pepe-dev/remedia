namespace ReMedia.App.Support;

using ReMedia.Core.Diagnostics;

/// <summary>
/// Tool logger that forwards all messages to a callback.
/// Used by the ViewModel to append to the log panel.
/// </summary>
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
