using Ciir.Core.Source;

namespace Ciir.Core.Conditions;

/// <summary>
/// A conditional/branching construct preserved as a static fact. The expression text is kept
/// verbatim; it is never interpreted as a business rule.
/// </summary>
public sealed record CiirCondition
{
    /// <summary>The construct kind.</summary>
    public required CiirConditionKind Kind { get; init; }

    /// <summary>The verbatim source text of the condition's controlling expression.</summary>
    public required string Expression { get; init; }

    /// <summary>The condition's location within the owning entity's source.</summary>
    public required CiirRange Location { get; init; }

    /// <summary>Fully qualified symbols read by the controlling expression, when statically resolvable.</summary>
    public IReadOnlyList<string> Reads { get; init; } = [];
}
