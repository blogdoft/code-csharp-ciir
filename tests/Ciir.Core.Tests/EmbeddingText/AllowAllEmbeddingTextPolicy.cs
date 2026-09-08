using Ciir.Core.Comments;
using Ciir.Core.Conditions;
using Ciir.Core.EmbeddingText;
using Ciir.Core.Relations;

namespace Ciir.Core.Tests.EmbeddingText;

/// <summary>Test double that includes everything, so section-ordering tests are not entangled with filtering.</summary>
internal sealed class AllowAllEmbeddingTextPolicy : IEmbeddingTextPolicy
{
    public bool ShouldIncludeRelation(CiirRelation relation) => true;

    public bool ShouldIncludeCondition(CiirCondition condition) => true;

    public bool ShouldIncludeComment(CiirComment comment) => true;
}
