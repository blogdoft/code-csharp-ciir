using Ciir.Core.Documentation;
using Ciir.Core.EmbeddingText;
using Ciir.Core.Relations;
using Ciir.Core.Symbols;
using Shouldly;

namespace Ciir.Core.Tests.EmbeddingText;

public class EmbeddingTextBuilderTests
{
    private static readonly AllowAllEmbeddingTextPolicy AllowAll = new();
    private static readonly DenyAllEmbeddingTextPolicy DenyAll = new();

    [Fact]
    public void Build_MatchesCiirSpecificationWorkedExample()
    {
        var document = new CiirDocument
        {
            Id = "sha256:test",
            Kind = CiirKind.Method,
            Language = "csharp",
            Project = "Payments.Application",
            Symbol = new CiirSymbol
            {
                Name = "AuthorizeAsync",
                QualifiedName = "Payments.Application.PaymentService.AuthorizeAsync",
                CanonicalName = "Payments.Application.PaymentService.AuthorizeAsync(Payments.Domain.Order,System.Threading.CancellationToken)",
                Container = "Payments.Application.PaymentService",
            },
            Method = new CiirMethodInfo
            {
                Accessibility = CiirAccessibility.Public,
                Modifiers = [CiirModifier.Async],
                Parameters =
                [
                    new CiirParameter { Name = "order", Type = "Payments.Domain.Order" },
                    new CiirParameter { Name = "cancellationToken", Type = "System.Threading.CancellationToken" },
                ],
                ReturnType = "System.Threading.Tasks.Task<Payments.Domain.PaymentResult>",
                EmbeddingReturnType = "Payments.Domain.PaymentResult",
            },
            Documentation = new CiirDocumentation
            {
                Format = CiirDocumentationFormat.XmlDoc,
                Source = CiirDocumentationSource.Declared,
                Summary = "Authorizes a payment for the given order.",
            },
            Relations =
            [
                Relation(CiirRelationKind.Reads, "Payments.Domain.Order.Total"),
                Relation(CiirRelationKind.Calls, "Payments.Domain.IPaymentGateway.AuthorizeAsync"),
                Relation(CiirRelationKind.Throws, "Payments.Domain.InvalidOrderException"),
            ],
        };

        var text = EmbeddingTextBuilder.Build(document, AllowAll);

        text.ShouldBe(
            "Entity: method\n" +
            "Qualified name: Payments.Application.PaymentService.AuthorizeAsync\n" +
            "Container: Payments.Application.PaymentService\n" +
            "Documentation: Authorizes a payment for the given order.\n" +
            "Parameters:\n" +
            "- order: Payments.Domain.Order\n" +
            "- cancellationToken: System.Threading.CancellationToken\n" +
            "Returns: Payments.Domain.PaymentResult\n" +
            "Reads:\n" +
            "- Payments.Domain.Order.Total\n" +
            "Calls:\n" +
            "- Payments.Domain.IPaymentGateway.AuthorizeAsync\n" +
            "Throws:\n" +
            "- Payments.Domain.InvalidOrderException");
    }

    [Fact]
    public void Build_OmitsContainer_WhenNotPresent()
    {
        var document = MinimalDocument(container: null);

        var text = EmbeddingTextBuilder.Build(document, AllowAll);

        text.ShouldNotContain("Container:");
    }

    [Fact]
    public void Build_OmitsAllOptionalSections_ForBareType()
    {
        var document = MinimalDocument(container: null);

        var text = EmbeddingTextBuilder.Build(document, AllowAll);

        text.ShouldBe("Entity: type\nQualified name: Payments.Domain.Order");
    }

    [Fact]
    public void Build_IsDeterministic_AcrossRepeatedCalls()
    {
        var document = MinimalDocument(container: "Payments.Domain");

        var first = EmbeddingTextBuilder.Build(document, AllowAll);
        var second = EmbeddingTextBuilder.Build(document, AllowAll);

        first.ShouldBe(second);
    }

    [Fact]
    public void Build_OmitsRelationSection_WhenPolicyExcludesEveryRelation()
    {
        var document = MinimalDocument(container: null) with
        {
            Relations = [Relation(CiirRelationKind.Calls, "System.String.IsNullOrEmpty")],
        };

        var text = EmbeddingTextBuilder.Build(document, DenyAll);

        text.ShouldNotContain("Calls:");
    }

    [Fact]
    public void Build_IncludesRelationSection_WhenPolicyAllowsIt()
    {
        var document = MinimalDocument(container: null) with
        {
            Relations = [Relation(CiirRelationKind.Calls, "Payments.Domain.IPaymentGateway.AuthorizeAsync")],
        };

        var text = EmbeddingTextBuilder.Build(document, AllowAll);

        text.ShouldContain("Calls:\n- Payments.Domain.IPaymentGateway.AuthorizeAsync");
    }

    private static CiirRelation Relation(CiirRelationKind kind, string targetSymbol) => new()
    {
        Kind = kind,
        Target = new CiirRelationTarget { Symbol = targetSymbol },
        Resolution = new CiirRelationResolution { Status = CiirResolutionStatus.Resolved, Origin = CiirResolutionOrigin.Project },
    };

    private static CiirDocument MinimalDocument(string? container) => new()
    {
        Id = "sha256:test",
        Kind = CiirKind.Type,
        Language = "csharp",
        Project = "Payments.Domain",
        Symbol = new CiirSymbol
        {
            Name = "Order",
            QualifiedName = "Payments.Domain.Order",
            CanonicalName = "Payments.Domain.Order",
            Container = container,
        },
    };
}
