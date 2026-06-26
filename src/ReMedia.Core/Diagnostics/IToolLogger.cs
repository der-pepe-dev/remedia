namespace ReMedia.Core.Diagnostics;

/// <summary>
/// Callback interface for capturing progress messages during tool workflows.
/// Both CLI and desktop hosts implement this to render logs consistently.
/// </summary>
public interface IToolLogger
{
    void LogMessage(string message);
}
