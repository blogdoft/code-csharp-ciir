using Ciir.Core.Comments;
using Ciir.Core.Conditions;
using Ciir.Core.EmbeddingText;
using Ciir.Core.Relations;

namespace Ciir.Core.Tests.EmbeddingText;

/// <summary>Test double that excludes everything, to verify filtered-out sections are omitted entirely.</summary>
internal sealed class DenyAllEmbeddingTextPolicy : IEmbeddingTextPolicy
{
    public bool ShouldIncludeRelation(CiirRelation relation) => false;

    public bool ShouldIncludeCondition(CiirCondition condition) => false;

    public bool ShouldIncludeComment(CiirComment comment) => false;
}
