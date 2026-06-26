namespace ReMedia.Cli.Support;

using ReMedia.Tooling.Configuration;

internal static class ToolPathResolver
{
    public static ExternalToolPaths ResolveFromEnvironment()
    {
        return ExternalToolPaths.ResolveFromEnvironment();
    }
}
