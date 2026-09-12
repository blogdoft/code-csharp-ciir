namespace Ciir.Configuration;

/// <summary>Normalizes a file path relative to the analysis root, matching <c>source.path</c>'s convention.</summary>
internal static class RelativePath
{
    /// <summary>Computes <paramref name="path"/> relative to <paramref name="rootDirectory"/>, forward-slash normalized.</summary>
    /// <param name="rootDirectory">The analysis root.</param>
    /// <param name="path">The full path to normalize.</param>
    public static string From(string rootDirectory, string path) =>
        Path.GetRelativePath(rootDirectory, path).Replace(Path.DirectorySeparatorChar, '/');
}
