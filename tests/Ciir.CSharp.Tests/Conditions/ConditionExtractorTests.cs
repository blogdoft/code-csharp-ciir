using Ciir.Core.Conditions;
using Ciir.CSharp.Conditions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Shouldly;

namespace Ciir.CSharp.Tests.Conditions;

public class ConditionExtractorTests
{
    [Fact]
    public void Extract_DoesNotThrow_ForIfBodyWithMultipleStatements()
    {
        // Regression test: a two-statement if-body ("log, then throw") previously crashed with
        // "Sequence contains more than one element" because guard-clause detection used
        // SyntaxList<T>.SingleOrDefault() without a predicate, which throws (rather than
        // returning null) once a block has more than one statement.
        var (body, semanticModel) = ParseMethodBody(
            """
            void Run(string? value)
            {
                if (value is null)
                {
                    Console.WriteLine("missing value");
                    throw new ArgumentNullException(nameof(value));
                }
            }
            """);

        var conditions = Should.NotThrow(() => ConditionExtractor.Extract(body, semanticModel));

        conditions.ShouldContain(c => c.Kind == CiirConditionKind.If);
    }

    [Fact]
    public void Extract_ClassifiesAsGuard_WhenIfBodyIsExactlyOneThrowStatement()
    {
        var (body, semanticModel) = ParseMethodBody(
            """
            void Run(string? value)
            {
                if (value is null)
                    throw new ArgumentNullException(nameof(value));
            }
            """);

        var conditions = ConditionExtractor.Extract(body, semanticModel);

        conditions.ShouldContain(c => c.Kind == CiirConditionKind.Guard);
    }

    [Fact]
    public void Extract_ClassifiesAsIf_WhenIfBodyIsEmptyBlock()
    {
        var (body, semanticModel) = ParseMethodBody(
            """
            void Run(string? value)
            {
                if (value is null)
                {
                }
            }
            """);

        var conditions = Should.NotThrow(() => ConditionExtractor.Extract(body, semanticModel));

        conditions.ShouldContain(c => c.Kind == CiirConditionKind.If);
    }

    private static (BlockSyntax Body, SemanticModel SemanticModel) ParseMethodBody(string methodSource)
    {
        var source =
            $$"""
            using System;

            class Fixture
            {
                {{methodSource}}
            }
            """;

        var tree = CSharpSyntaxTree.ParseText(source);
        var compilation = CSharpCompilation.Create(
            "ConditionExtractorTests",
            [tree],
            [MetadataReference.CreateFromFile(typeof(object).Assembly.Location)],
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        var semanticModel = compilation.GetSemanticModel(tree);
        var body = tree.GetRoot()
            .DescendantNodes()
            .OfType<MethodDeclarationSyntax>()
            .Single()
            .Body!;

        return (body, semanticModel);
    }
}
