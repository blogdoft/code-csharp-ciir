using Ciir.Core.Comments;
using Ciir.Core.Source;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace Ciir.CSharp.Comments;

/// <summary>
/// Extracts ordinary (non-doc) source comments found within a declaration's span, classifying
/// them as TODO/FIXME/warning/note by a leading marker, and preserving them separately from
/// formal XML documentation.
/// </summary>
internal static class TriviaCommentExtractor
{
    public static IReadOnlyList<CiirComment> Extract(SyntaxNode node)
    {
        var comments = new List<CiirComment>();

        foreach (var trivia in node.DescendantTrivia())
        {
            var baseKind = trivia.Kind() switch
            {
                SyntaxKind.SingleLineCommentTrivia => CiirCommentKind.Line,
                SyntaxKind.MultiLineCommentTrivia => CiirCommentKind.Block,
                _ => (CiirCommentKind?)null,
            };

            if (baseKind is not { } kind)
            {
                continue;
            }

            var text = CleanCommentText(trivia.ToString(), kind);
            if (text.Length == 0)
            {
                continue;
            }

            var lineSpan = trivia.SyntaxTree!.GetLineSpan(trivia.Span);

            comments.Add(new CiirComment
            {
                Kind = ClassifyKind(text) ?? kind,
                Text = text,
                Location = new CiirRange
                {
                    StartLine = lineSpan.StartLinePosition.Line + 1,
                    StartColumn = lineSpan.StartLinePosition.Character + 1,
                    EndLine = lineSpan.EndLinePosition.Line + 1,
                    EndColumn = lineSpan.EndLinePosition.Character + 1,
                },
            });
        }

        return comments;
    }

    private static CiirCommentKind? ClassifyKind(string text)
    {
        if (text.StartsWith("TODO", StringComparison.OrdinalIgnoreCase))
        {
            return CiirCommentKind.Todo;
        }

        if (text.StartsWith("FIXME", StringComparison.OrdinalIgnoreCase))
        {
            return CiirCommentKind.Fixme;
        }

        if (text.StartsWith("WARNING", StringComparison.OrdinalIgnoreCase))
        {
            return CiirCommentKind.Warning;
        }

        if (text.StartsWith("NOTE", StringComparison.OrdinalIgnoreCase))
        {
            return CiirCommentKind.Note;
        }

        return null;
    }

    private static string CleanCommentText(string raw, CiirCommentKind kind) => kind == CiirCommentKind.Line
        ? raw.TrimStart('/').Trim()
        : raw.TrimStart('/', '*').TrimEnd('*', '/').Trim();
}
