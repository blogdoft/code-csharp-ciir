using Ciir.Core.Comments;
using Ciir.Core.Conditions;
using Ciir.Core.Relations;

namespace Ciir.Core.EmbeddingText;

/// <summary>
/// Decides which relations, conditions and comments are relevant enough to appear in
/// <c>embeddingText</c>. Keeping this decision behind a single, testable component avoids
/// scattering noise-filtering <c>if</c>s throughout the language analyzer (see CIIR spec §39).
/// </summary>
public interface IEmbeddingTextPolicy
{
    /// <summary>Whether <paramref name="relation"/> is relevant enough to include in the embedding text.</summary>
    /// <param name="relation">The candidate relation.</param>
    bool ShouldIncludeRelation(CiirRelation relation);

    /// <summary>Whether <paramref name="condition"/> is relevant enough to include in the embedding text.</summary>
    /// <param name="condition">The candidate condition.</param>
    bool ShouldIncludeCondition(CiirCondition condition);

    /// <summary>Whether <paramref name="comment"/> is relevant enough to include in the embedding text.</summary>
    /// <param name="comment">The candidate comment.</param>
    bool ShouldIncludeComment(CiirComment comment);
}
