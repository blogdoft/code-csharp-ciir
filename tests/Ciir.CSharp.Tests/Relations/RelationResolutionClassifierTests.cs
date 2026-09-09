using Ciir.Core.Relations;
using Ciir.CSharp.Relations;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Shouldly;

namespace Ciir.CSharp.Tests.Relations;

public class RelationResolutionClassifierTests
{
    [Fact]
    public void ClassifyOrigin_ReturnsProject_ForSymbolInCurrentAssembly()
    {
        var symbol = GetFixtureType("Current");
        var context = CreateContext(currentAssemblyName: "Current");

        RelationResolutionClassifier.ClassifyOrigin(symbol, context).ShouldBe(CiirResolutionOrigin.Project);
    }

    [Fact]
    public void ClassifyOrigin_ReturnsSolution_ForSymbolInAnotherAnalyzedProject()
    {
        var symbol = GetFixtureType("Other");
        var context = CreateContext(currentAssemblyName: "Current", knownAssemblyName: "Other", knownProjectName: "OtherProject");

        RelationResolutionClassifier.ClassifyOrigin(symbol, context).ShouldBe(CiirResolutionOrigin.Solution);
    }

    [Fact]
    public void ClassifyOrigin_ReturnsDependency_ForSymbolInUnknownAssembly()
    {
        MetadataReference[] references =
        [
            MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(Should).Assembly.Location),
        ];
        var compilation = CSharpCompilation.Create("Current", [], references, new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        var shouldType = compilation.GetTypeByMetadataName("Shouldly.Should") ?? throw new InvalidOperationException("Shouldly.Should not found.");
        var context = CreateContext(currentAssemblyName: "Current");

        RelationResolutionClassifier.ClassifyOrigin(shouldType, context).ShouldBe(CiirResolutionOrigin.Dependency);
    }

    [Fact]
    public void ClassifyOrigin_ReturnsFramework_ForBclSymbol()
    {
        var (compilation, _) = Compile("Current", "class Fixture { }");
        var stringType = compilation.GetSpecialType(SpecialType.System_String);

        var context = CreateContext(currentAssemblyName: "Current");

        RelationResolutionClassifier.ClassifyOrigin(stringType, context).ShouldBe(CiirResolutionOrigin.Framework);
    }

    [Fact]
    public void ClassifyOrigin_ReturnsUnknown_WhenContainingAssemblyIsNull()
    {
        var (compilation, _) = Compile("Current", "class Fixture { }");
        var arrayType = compilation.CreateArrayTypeSymbol(compilation.GetSpecialType(SpecialType.System_Int32));

        arrayType.ContainingAssembly.ShouldBeNull();

        var context = CreateContext(currentAssemblyName: "Current");

        RelationResolutionClassifier.ClassifyOrigin(arrayType, context).ShouldBe(CiirResolutionOrigin.Unknown);
    }

    [Fact]
    public void Classify_ReturnsResolved_ForSolutionOrigin()
    {
        var symbolInfo = CreateResolvedSymbolInfo("Other");
        var context = CreateContext(currentAssemblyName: "Current", knownAssemblyName: "Other", knownProjectName: "OtherProject");

        var resolution = RelationResolutionClassifier.Classify(symbolInfo, context);

        resolution.Status.ShouldBe(CiirResolutionStatus.Resolved);
        resolution.Origin.ShouldBe(CiirResolutionOrigin.Solution);
    }

    private static INamedTypeSymbol GetFixtureType(string assemblyName)
    {
        var (compilation, tree) = Compile(assemblyName, "class Fixture { }");
        var semanticModel = compilation.GetSemanticModel(tree);
        var declaration = tree.GetRoot().DescendantNodes().OfType<ClassDeclarationSyntax>().Single();
        return (INamedTypeSymbol)semanticModel.GetDeclaredSymbol(declaration)!;
    }

    private static SymbolInfo CreateResolvedSymbolInfo(string assemblyName)
    {
        const string Source =
            """
            class Fixture { }
            class Probe { Fixture F() => default!; }
            """;
        var (compilation, tree) = Compile(assemblyName, Source);

        var semanticModel = compilation.GetSemanticModel(tree);
        var returnType = tree.GetRoot()
            .DescendantNodes()
            .OfType<MethodDeclarationSyntax>()
            .Single(m => string.Equals(m.Identifier.ValueText, "F", StringComparison.Ordinal))
            .ReturnType;

        return semanticModel.GetSymbolInfo(returnType);
    }

    private static (Compilation Compilation, SyntaxTree Tree) Compile(string assemblyName, string source)
    {
        var tree = CSharpSyntaxTree.ParseText(source);
        var compilation = CSharpCompilation.Create(
            assemblyName,
            [tree],
            [MetadataReference.CreateFromFile(typeof(object).Assembly.Location)],
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        return (compilation, tree);
    }

    private static RelationResolutionContext CreateContext(string currentAssemblyName, string? knownAssemblyName = null, string? knownProjectName = null)
    {
        var map = new Dictionary<string, string>(StringComparer.Ordinal) { [currentAssemblyName] = currentAssemblyName };
        if (knownAssemblyName is not null && knownProjectName is not null)
        {
            map[knownAssemblyName] = knownProjectName;
        }

        return new RelationResolutionContext
        {
            CurrentAssemblyName = currentAssemblyName,
            CurrentProjectName = currentAssemblyName,
            AssemblyNameToProjectName = map,
        };
    }
}
