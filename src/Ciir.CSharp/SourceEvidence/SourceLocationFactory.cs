using Ciir.Core.Hashing;
using Ciir.Core.Source;
using Microsoft.CodeAnalysis;

namespace Ciir.CSharp.SourceEvidence;

/// <summary>
/// Builds <see cref="CiirSourceLocation"/> values from Roslyn syntax nodes. Paths are always
/// relative to the analysis root (the directory the caller originally resolved the input to, not
/// necessarily the declaring file's own project directory) and normalized to forward slashes, so
/// the same source produces identical output regardless of the host operating system.
/// </summary>
internal static class SourceLocationFactory
{
    public static CiirSourceLocation Create(SyntaxNode node, string rootDirectory, bool includeSource)
    {
        ArgumentNullException.ThrowIfNull(node);

        var lineSpan = node.SyntaxTree.GetLineSpan(node.Span);
        var text = node.ToString();

        return new CiirSourceLocation
        {
            Path = ToRelativePath(rootDirectory, node.SyntaxTree.FilePath),
            StartLine = lineSpan.StartLinePosition.Line + 1,
            StartColumn = lineSpan.StartLinePosition.Character + 1,
            EndLine = lineSpan.EndLinePosition.Line + 1,
            EndColumn = lineSpan.EndLinePosition.Character + 1,
            Hash = Sha256Text.ComputePrefixedHash(text),
            Text = includeSource ? text : null,
        };
    }

    private static string ToRelativePath(string rootDirectory, string filePath) =>
        Path.GetRelativePath(rootDirectory, filePath).Replace(Path.DirectorySeparatorChar, '/');
}
