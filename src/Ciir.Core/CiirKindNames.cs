namespace Ciir.Core;

/// <summary>
/// Canonical lowercase string tokens for <see cref="CiirKind"/> values, shared by identity
/// hashing (<see cref="Identity.CiirIdentity"/>) and embedding text generation
/// (<see cref="EmbeddingText.EmbeddingTextBuilder"/>) so both always agree on the same spelling.
/// </summary>
public static class CiirKindNames
{
    /// <summary>Returns the canonical lowercase token for <paramref name="kind"/> (e.g. <c>"method"</c>).</summary>
    /// <param name="kind">The kind to convert.</param>
    public static string ToToken(CiirKind kind) => kind switch
    {
        CiirKind.Project => "project",
        CiirKind.Namespace => "namespace",
        CiirKind.Type => "type",
        CiirKind.Method => "method",
        CiirKind.Constructor => "constructor",
        CiirKind.Property => "property",
        CiirKind.Field => "field",
        CiirKind.Event => "event",
        CiirKind.File => "file",
        CiirKind.Configuration => "configuration",
        CiirKind.ConfigurationKey => "configuration_key",
        _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, message: null),
    };
}
