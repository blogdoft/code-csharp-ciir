namespace Ciir.Core;

/// <summary>
/// The kind of software entity a <see cref="CiirDocument"/> represents.
/// Only the kinds required for the C# v1 generator are defined; the CIIR contract
/// reserves room for additional kinds (e.g. database/configuration/endpoint entities)
/// for future language generators.
/// </summary>
public enum CiirKind
{
    /// <summary>A logical project (e.g. a .csproj).</summary>
    Project,

    /// <summary>A namespace declaration.</summary>
    Namespace,

    /// <summary>A type declaration (class, interface, struct, record, enum, delegate).</summary>
    Type,

    /// <summary>A method declaration.</summary>
    Method,

    /// <summary>A constructor declaration.</summary>
    Constructor,

    /// <summary>A property declaration.</summary>
    Property,

    /// <summary>A field declaration.</summary>
    Field,

    /// <summary>An event declaration.</summary>
    Event,
}
