using Ciir.Core.Hashing;

namespace Ciir.Core.Identity;

/// <summary>
/// Computes deterministic CIIR document identities. Two analyses of the same semantic symbol
/// must always produce the same <c>id</c>; overloads and unrelated symbols must never collide.
/// This algorithm is the reference implementation of the identity rule described in the CIIR
/// specification and must be reproducible by generators for other languages.
/// </summary>
public static class CiirIdentity
{
    private const char Separator = '|';

    /// <summary>
    /// Computes the deterministic <c>sha256:&lt;hex&gt;</c> id for a CIIR document from its
    /// canonical identity components.
    /// </summary>
    /// <param name="language">The source language, e.g. <c>"csharp"</c>.</param>
    /// <param name="projectIdentity">The logical project identity the symbol belongs to.</param>
    /// <param name="kind">The kind of entity being identified.</param>
    /// <param name="canonicalSymbolIdentity">
    /// The fully qualified, overload-disambiguating identity of the symbol (e.g. a method's
    /// canonical name including parameter types).
    /// </param>
    public static string ComputeId(string language, string projectIdentity, CiirKind kind, string canonicalSymbolIdentity)
    {
        var canonicalKey = BuildCanonicalKey(language, projectIdentity, kind, canonicalSymbolIdentity);
        return Sha256Text.ComputePrefixedHash(canonicalKey);
    }

    /// <summary>Builds the canonical key that gets hashed to produce a document id.</summary>
    /// <param name="language">The source language, e.g. <c>"csharp"</c>.</param>
    /// <param name="projectIdentity">The logical project identity the symbol belongs to.</param>
    /// <param name="kind">The kind of entity being identified.</param>
    /// <param name="canonicalSymbolIdentity">The overload-disambiguating identity of the symbol.</param>
    public static string BuildCanonicalKey(string language, string projectIdentity, CiirKind kind, string canonicalSymbolIdentity)
    {
        ArgumentNullException.ThrowIfNull(language);
        ArgumentNullException.ThrowIfNull(projectIdentity);
        ArgumentNullException.ThrowIfNull(canonicalSymbolIdentity);

        return string.Join(Separator, language, projectIdentity, CiirKindNames.ToToken(kind), canonicalSymbolIdentity);
    }
}
