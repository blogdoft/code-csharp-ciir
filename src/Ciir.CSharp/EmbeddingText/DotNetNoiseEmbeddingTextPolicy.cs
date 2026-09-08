using Ciir.Core.Comments;
using Ciir.Core.Conditions;
using Ciir.Core.EmbeddingText;
using Ciir.Core.Relations;

namespace Ciir.CSharp.EmbeddingText;

/// <summary>
/// The C#/.NET-specific <see cref="IEmbeddingTextPolicy"/>: excludes calls to extremely generic
/// BCL/framework members from <c>embeddingText</c> (they are still preserved in
/// <c>relations</c>), and only surfaces comments carrying an explicit marker (TODO/FIXME/warning/note).
/// The exclusion list is intentionally hardcoded for v1 rather than externally configurable —
/// nothing in the specification calls for user-facing configurability here, only for the
/// filtering decision to live in one coherent, testable place.
/// </summary>
public sealed class DotNetNoiseEmbeddingTextPolicy : IEmbeddingTextPolicy
{
    private static readonly string[] NoisyCallPrefixes =
    [
        "System.String.IsNullOrEmpty",
        "System.String.IsNullOrWhiteSpace",
        "System.String.Format",
        "System.String.Concat",
        "System.String.Join",
        "System.Threading.Tasks.Task.WhenAll",
        "System.Threading.Tasks.Task.WhenAny",
        "System.Threading.Tasks.Task.Delay",
        "System.Threading.Tasks.Task.Run",
        "System.Threading.Tasks.Task.FromResult",
        "System.Linq.Enumerable.ToList",
        "System.Linq.Enumerable.ToArray",
        "System.Linq.Enumerable.Select",
        "System.Linq.Enumerable.Where",
        "System.Linq.Enumerable.Any",
        "System.Linq.Enumerable.All",
        "System.Linq.Enumerable.FirstOrDefault",
        "System.Linq.Enumerable.First",
        "System.Linq.Enumerable.Count",
        "Microsoft.Extensions.Logging.LoggerExtensions",
        "Microsoft.Extensions.Logging.ILogger",
        "System.ArgumentNullException.ThrowIfNull",
        "System.Console.WriteLine",
    ];

    /// <inheritdoc />
    public bool ShouldIncludeRelation(CiirRelation relation)
    {
        ArgumentNullException.ThrowIfNull(relation);

        if (relation.Kind != CiirRelationKind.Calls)
        {
            return true;
        }

        return !NoisyCallPrefixes.Any(prefix => relation.Target.Symbol.StartsWith(prefix, StringComparison.Ordinal));
    }

    /// <inheritdoc />
    public bool ShouldIncludeCondition(CiirCondition condition) => true;

    /// <inheritdoc />
    public bool ShouldIncludeComment(CiirComment comment)
    {
        ArgumentNullException.ThrowIfNull(comment);

        return comment.Kind is CiirCommentKind.Todo or CiirCommentKind.Fixme or CiirCommentKind.Warning or CiirCommentKind.Note;
    }
}
