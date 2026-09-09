using Ciir.Core;
using Ciir.CSharp.SymbolMapping;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Shouldly;

namespace Ciir.CSharp.Tests.SymbolMapping;

public class SymbolDocumentClassifierTests
{
    [Fact]
    public void TryClassify_ReturnsType_ForPlainClass()
    {
        var (compilation, tree) = Compile(
            """
            class Fixture
            {
            }
            """);

        var type = GetType(compilation, tree, "Fixture");

        var classified = SymbolDocumentClassifier.TryClassify(type, out var kind, out var identity);

        classified.ShouldBeTrue();
        kind.ShouldBe(CiirKind.Type);
        identity.ShouldBe(SymbolNaming.QualifiedName(type));
    }

    [Fact]
    public void TryClassify_ReturnsFalse_ForRecordPositionalProperty()
    {
        var (compilation, tree) = Compile(
            """
            record Money(decimal Amount);
            """);

        var moneyType = GetType(compilation, tree, "Money");
        var amountProperty = moneyType.GetMembers("Amount").OfType<IPropertySymbol>().Single();

        SymbolDocumentClassifier.TryClassify(moneyType, out var typeKind, out _).ShouldBeTrue();
        typeKind.ShouldBe(CiirKind.Type);

        SymbolDocumentClassifier.TryClassify(amountProperty, out _, out _).ShouldBeFalse();
    }

    [Fact]
    public void TryClassify_ReturnsFalse_ForIndexer()
    {
        var (compilation, tree) = Compile(
            """
            class Fixture
            {
                public int this[int i]
                {
                    get => i;
                    set { }
                }
            }
            """);

        var type = GetType(compilation, tree, "Fixture");
        var indexer = type.GetMembers().OfType<IPropertySymbol>().Single(p => p.IsIndexer);

        SymbolDocumentClassifier.TryClassify(indexer, out _, out _).ShouldBeFalse();
    }

    [Fact]
    public void TryClassify_ReturnsFalse_ForAutoPropertyBackingField()
    {
        var (compilation, tree) = Compile(
            """
            class Fixture
            {
                public int Value { get; set; }
            }
            """);

        var type = GetType(compilation, tree, "Fixture");
        var backingField = type.GetMembers().OfType<IFieldSymbol>().Single(f => f.IsImplicitlyDeclared);

        SymbolDocumentClassifier.TryClassify(backingField, out _, out _).ShouldBeFalse();
    }

    [Fact]
    public void TryClassify_ReturnsFalse_ForLocalFunction()
    {
        var (compilation, tree) = Compile(
            """
            class Fixture
            {
                void Run()
                {
                    void Local()
                    {
                    }
                }
            }
            """);

        var semanticModel = compilation.GetSemanticModel(tree);
        var localFunctionSyntax = tree.GetRoot(TestContext.Current.CancellationToken)
            .DescendantNodes()
            .OfType<LocalFunctionStatementSyntax>()
            .Single();
        var localFunction = (IMethodSymbol)semanticModel.GetDeclaredSymbol(localFunctionSyntax, TestContext.Current.CancellationToken)!;

        localFunction.MethodKind.ShouldBe(MethodKind.LocalFunction);
        SymbolDocumentClassifier.TryClassify(localFunction, out _, out _).ShouldBeFalse();
    }

    [Fact]
    public void TryClassify_ReturnsFalse_ForMetadataOnlySymbol()
    {
        var (compilation, _) = Compile("class Fixture { }");

        var stringType = compilation.GetSpecialType(SpecialType.System_String);

        stringType.DeclaringSyntaxReferences.IsEmpty.ShouldBeTrue();
        SymbolDocumentClassifier.TryClassify(stringType, out _, out _).ShouldBeFalse();
    }

    [Fact]
    public void TryClassify_NormalizesConstructedGenericMember_ToUnboundIdentity()
    {
        var (compilation, tree) = Compile(
            """
            class Wrapper<T>
            {
                public T Value { get; set; } = default!;
            }
            """);

        var wrapperType = GetType(compilation, tree, "Wrapper");
        var unboundValue = wrapperType.GetMembers("Value").OfType<IPropertySymbol>().Single();

        var intType = compilation.GetSpecialType(SpecialType.System_Int32);
        var constructedWrapper = wrapperType.Construct(intType);
        var constructedValue = constructedWrapper.GetMembers("Value").OfType<IPropertySymbol>().Single();

        SymbolDocumentClassifier.TryClassify(constructedWrapper, out var typeKind, out var typeIdentity).ShouldBeTrue();
        typeKind.ShouldBe(CiirKind.Type);
        typeIdentity.ShouldBe(SymbolNaming.QualifiedName(wrapperType));

        SymbolDocumentClassifier.TryClassify(constructedValue, out var memberKind, out var memberIdentity).ShouldBeTrue();
        memberKind.ShouldBe(CiirKind.Property);
        memberIdentity.ShouldBe(SymbolNaming.QualifiedName(unboundValue));
    }

    private static (Compilation Compilation, SyntaxTree Tree) Compile(string source)
    {
        var tree = CSharpSyntaxTree.ParseText(source);
        var compilation = CSharpCompilation.Create(
            "SymbolDocumentClassifierTests",
            [tree],
            [MetadataReference.CreateFromFile(typeof(object).Assembly.Location)],
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        return (compilation, tree);
    }

    private static INamedTypeSymbol GetType(Compilation compilation, SyntaxTree tree, string name)
    {
        var semanticModel = compilation.GetSemanticModel(tree);
        var declaration = tree.GetRoot()
            .DescendantNodes()
            .OfType<BaseTypeDeclarationSyntax>()
            .Single(d => string.Equals(d.Identifier.ValueText, name, StringComparison.Ordinal));

        return (INamedTypeSymbol)semanticModel.GetDeclaredSymbol(declaration)!;
    }
}
