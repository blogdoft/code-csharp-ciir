namespace Ciir.Core;

/// <summary>
/// The kind of software entity a <see cref="CiirDocument"/> represents.
/// The C# v1 generator produces <see cref="Project"/> through <see cref="Event"/>; the
/// configuration/YAML generator produces <see cref="File"/>, <see cref="Configuration"/> and
/// <see cref="ConfigurationKey"/>. The CIIR contract reserves room for further kinds
/// (e.g. database/endpoint entities) for future language generators.
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

    /// <summary>A generic file captured only as metadata, without structural parsing (e.g. a YAML file).</summary>
    File,

    /// <summary>A structured configuration file whose keys are individually represented (e.g. an appsettings.json).</summary>
    Configuration,

    /// <summary>One flattened key within a <see cref="Configuration"/> document.</summary>
    ConfigurationKey,
}
