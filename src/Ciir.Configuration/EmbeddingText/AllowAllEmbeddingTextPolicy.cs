using Ciir.Core.Comments;
using Ciir.Core.Conditions;
using Ciir.Core.EmbeddingText;
using Ciir.Core.Relations;

namespace Ciir.Configuration.EmbeddingText;

/// <summary>
/// The configuration/YAML generator's <see cref="IEmbeddingTextPolicy"/>: configuration and file
/// documents never carry relations, conditions or comments, so there is no noise to filter.
/// </summary>
internal sealed class AllowAllEmbeddingTextPolicy : IEmbeddingTextPolicy
{
    /// <inheritdoc />
    public bool ShouldIncludeRelation(CiirRelation relation) => true;

    /// <inheritdoc />
    public bool ShouldIncludeCondition(CiirCondition condition) => true;

    /// <inheritdoc />
    public bool ShouldIncludeComment(CiirComment comment) => true;
}
