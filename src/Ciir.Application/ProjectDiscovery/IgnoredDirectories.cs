namespace Ciir.Application.ProjectDiscovery;

/// <summary>Directory names skipped by every filesystem walk over an analyzed root.</summary>
public static class IgnoredDirectories
{
    private static readonly string[] Names = ["bin", "obj", ".git", ".vs", "node_modules"];

    /// <summary>Whether <paramref name="directoryName"/> (not a full path) should be skipped.</summary>
    /// <param name="directoryName">The directory's own name, not its full path.</param>
    public static bool IsIgnored(string directoryName) =>
        Names.Contains(directoryName, StringComparer.OrdinalIgnoreCase);
}
