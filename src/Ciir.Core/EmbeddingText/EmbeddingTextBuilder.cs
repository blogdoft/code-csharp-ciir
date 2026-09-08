using Ciir.Core.Relations;

namespace Ciir.Core.EmbeddingText;

/// <summary>
/// Builds the deterministic <c>embeddingText</c> projection for a <see cref="CiirDocument"/>,
/// following the <c>semantic-v1</c> section order: Entity, Qualified name, Container,
/// Documentation, Parameters, Returns, Relevant comments, Reads, Writes, Calls, Constructs,
/// Throws, Conditions. Empty sections are omitted entirely.
/// </summary>
/// <remarks>
/// The CIIR specification's illustrative order also names a "Signature" section between
/// Documentation and Parameters, but no worked example shows its content, and the concrete
/// example payloads never include one. This builder therefore does not emit a "Signature" line;
/// the same structural information is already conveyed by the Parameters/Returns sections.
/// </remarks>
public static class EmbeddingTextBuilder
{
    /// <summary>The name of this deterministic generation strategy.</summary>
    public const string StrategyName = "semantic-v1";

    /// <summary>Builds the embedding text for <paramref name="document"/> using <paramref name="policy"/> to filter noise.</summary>
    /// <param name="document">The document to build embedding text for.</param>
    /// <param name="policy">The noise-filtering policy to apply.</param>
    public static string Build(CiirDocument document, IEmbeddingTextPolicy policy)
    {
        ArgumentNullException.ThrowIfNull(document);
        ArgumentNullException.ThrowIfNull(policy);

        var lines = new List<string>
        {
            $"Entity: {CiirKindNames.ToToken(document.Kind)}",
            $"Qualified name: {document.Symbol.QualifiedName}",
        };

        if (!string.IsNullOrEmpty(document.Symbol.Container))
        {
            lines.Add($"Container: {document.Symbol.Container}");
        }

        AppendDocumentation(lines, document);
        AppendParameters(lines, document);
        AppendReturns(lines, document);
        AppendComments(lines, document, policy);
        AppendRelationSection(lines, document, policy, CiirRelationKind.Reads, "Reads");
        AppendRelationSection(lines, document, policy, CiirRelationKind.Writes, "Writes");
        AppendRelationSection(lines, document, policy, CiirRelationKind.Calls, "Calls");
        AppendRelationSection(lines, document, policy, CiirRelationKind.Constructs, "Constructs");
        AppendRelationSection(lines, document, policy, CiirRelationKind.Throws, "Throws");
        AppendConditions(lines, document, policy);

        return string.Join('\n', lines);
    }

    private static void AppendDocumentation(List<string> lines, CiirDocument document)
    {
        var documentation = document.Documentation;
        if (documentation is null)
        {
            return;
        }

        if (!string.IsNullOrEmpty(documentation.Summary))
        {
            lines.Add($"Documentation: {documentation.Summary}");
        }

        if (!string.IsNullOrEmpty(documentation.Remarks))
        {
            lines.Add($"Remarks: {documentation.Remarks}");
        }
    }

    private static void AppendParameters(List<string> lines, CiirDocument document)
    {
        var parameters = document.Method?.Parameters;
        if (parameters is not { Count: > 0 })
        {
            return;
        }

        lines.Add("Parameters:");
        foreach (var parameter in parameters)
        {
            lines.Add($"- {parameter.Name}: {parameter.Type}");
        }
    }

    private static void AppendReturns(List<string> lines, CiirDocument document)
    {
        var returnType = document.Method?.EmbeddingReturnType ?? document.Method?.ReturnType;
        if (string.IsNullOrEmpty(returnType))
        {
            return;
        }

        lines.Add($"Returns: {returnType}");
    }

    private static void AppendComments(List<string> lines, CiirDocument document, IEmbeddingTextPolicy policy)
    {
        var relevant = document.Comments.Where(policy.ShouldIncludeComment).ToArray();
        if (relevant.Length == 0)
        {
            return;
        }

        lines.Add("Comments:");
        foreach (var comment in relevant)
        {
            lines.Add($"- {comment.Text}");
        }
    }

    private static void AppendRelationSection(
        List<string> lines,
        CiirDocument document,
        IEmbeddingTextPolicy policy,
        CiirRelationKind kind,
        string label)
    {
        var relevant = document.Relations
            .Where(relation => relation.Kind == kind && policy.ShouldIncludeRelation(relation))
            .ToArray();

        if (relevant.Length == 0)
        {
            return;
        }

        lines.Add($"{label}:");
        foreach (var relation in relevant)
        {
            lines.Add($"- {relation.Target.Symbol}");
        }
    }

    private static void AppendConditions(List<string> lines, CiirDocument document, IEmbeddingTextPolicy policy)
    {
        var relevant = document.Conditions.Where(policy.ShouldIncludeCondition).ToArray();
        if (relevant.Length == 0)
        {
            return;
        }

        lines.Add("Conditions:");
        foreach (var condition in relevant)
        {
            lines.Add($"- {condition.Expression}");
        }
    }
}
