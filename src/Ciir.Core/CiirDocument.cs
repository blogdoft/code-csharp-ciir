using Ciir.Core.Comments;
using Ciir.Core.Conditions;
using Ciir.Core.Configuration;
using Ciir.Core.ControlFlow;
using Ciir.Core.Documentation;
using Ciir.Core.Extensions;
using Ciir.Core.Relations;
using Ciir.Core.Source;
using Ciir.Core.Symbols;

namespace Ciir.Core;

/// <summary>
/// The root envelope of a single CIIR record. Every statically observable software entity
/// (a project, a namespace, a type, a member, ...) is represented as exactly one
/// <see cref="CiirDocument"/>, serialized as one line of the <c>ciir.jsonl</c> output.
/// </summary>
public sealed record CiirDocument
{
    /// <summary>The CIIR contract version this document conforms to.</summary>
    public string SchemaVersion { get; init; } = Ciir.Core.SchemaVersion.Current;

    /// <summary>The deterministic document id (see <see cref="Identity.CiirIdentity"/>).</summary>
    public required string Id { get; init; }

    /// <summary>The entity kind.</summary>
    public required CiirKind Kind { get; init; }

    /// <summary>The source language, e.g. <c>"csharp"</c>.</summary>
    public required string Language { get; init; }

    /// <summary>The logical project this entity belongs to.</summary>
    public required string Project { get; init; }

    /// <summary>Identity information for the represented entity.</summary>
    public required CiirSymbol Symbol { get; init; }

    /// <summary>The primary declaration source location.</summary>
    public CiirSourceLocation? Source { get; init; }

    /// <summary>
    /// Additional declaration locations for entities with more than one physical declaration
    /// (C# <c>partial</c> types/methods). A partial entity is still represented by exactly one
    /// <see cref="CiirDocument"/>; <see cref="Source"/> holds its primary declaration.
    /// </summary>
    public IReadOnlyList<CiirSourceLocation> AdditionalSourceLocations { get; init; } = [];

    /// <summary>Formal documentation extracted for this entity.</summary>
    public CiirDocumentation? Documentation { get; init; }

    /// <summary>Ordinary source comments associated with this entity.</summary>
    public IReadOnlyList<CiirComment> Comments { get; init; } = [];

    /// <summary>Statically observable relationships from this entity to other symbols.</summary>
    public IReadOnlyList<CiirRelation> Relations { get; init; } = [];

    /// <summary>Conditional/branching constructs found in this entity's body.</summary>
    public IReadOnlyList<CiirCondition> Conditions { get; init; } = [];

    /// <summary>Aggregate control-flow metrics, for method-like entities.</summary>
    public CiirControlFlow? ControlFlow { get; init; }

    /// <summary>Present when <see cref="Kind"/> is <see cref="CiirKind.Type"/>.</summary>
    public CiirTypeInfo? Type { get; init; }

    /// <summary>Present when <see cref="Kind"/> is <see cref="CiirKind.Method"/> or <see cref="CiirKind.Constructor"/>.</summary>
    public CiirMethodInfo? Method { get; init; }

    /// <summary>Present when <see cref="Kind"/> is <see cref="CiirKind.Property"/>.</summary>
    public CiirPropertyInfo? Property { get; init; }

    /// <summary>Present when <see cref="Kind"/> is <see cref="CiirKind.Field"/>.</summary>
    public CiirFieldInfo? Field { get; init; }

    /// <summary>Present when <see cref="Kind"/> is <see cref="CiirKind.Event"/>.</summary>
    public CiirEventInfo? Event { get; init; }

    /// <summary>Present when <see cref="Kind"/> is <see cref="CiirKind.ConfigurationKey"/>.</summary>
    public CiirConfigurationKeyInfo? ConfigurationKey { get; init; }

    /// <summary>Present when <see cref="Kind"/> is <see cref="CiirKind.File"/>.</summary>
    public CiirFileInfo? File { get; init; }

    /// <summary>The generated semantic-projection text used for embedding generation.</summary>
    public string? EmbeddingText { get; init; }

    /// <summary>The name of the strategy used to produce <see cref="EmbeddingText"/>.</summary>
    public string EmbeddingTextStrategy { get; init; } = Ciir.Core.EmbeddingText.EmbeddingTextBuilder.StrategyName;

    /// <summary>SHA-256 hash (<c>sha256:&lt;hex&gt;</c>) of <see cref="EmbeddingText"/>.</summary>
    public string? EmbeddingTextHash { get; init; }

    /// <summary>Language-specific data that does not belong in the universal CIIR model.</summary>
    public CiirExtensions? Extensions { get; init; }
}
