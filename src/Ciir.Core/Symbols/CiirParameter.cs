namespace Ciir.Core.Symbols;

/// <summary>A method or constructor parameter, in declaration order.</summary>
public sealed record CiirParameter
{
    /// <summary>The parameter name.</summary>
    public required string Name { get; init; }

    /// <summary>The parameter's fully qualified type name.</summary>
    public required string Type { get; init; }
}
