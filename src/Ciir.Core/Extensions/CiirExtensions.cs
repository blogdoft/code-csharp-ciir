namespace Ciir.Core.Extensions;

/// <summary>
/// Language-specific data that does not belong in the universal CIIR model. C#-only details
/// (e.g. whether a record is a <c>record struct</c>) live under <see cref="Csharp"/> rather than
/// as first-class Core properties, so Core does not accumulate single-language concepts.
/// </summary>
public sealed record CiirExtensions
{
    /// <summary>C#-specific extension data, keyed by a short property name.</summary>
    public IReadOnlyDictionary<string, object?> Csharp { get; init; } = new Dictionary<string, object?>(StringComparer.Ordinal);
}
