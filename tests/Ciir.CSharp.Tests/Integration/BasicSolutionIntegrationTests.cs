using Ciir.Application.Model;
using Ciir.Core;
using Ciir.Core.Conditions;
using Ciir.Core.Relations;
using Ciir.CSharp.Relations;
using Ciir.CSharp.Workspace;
using Ciir.Serialization.Json;
using Json.Schema;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Shouldly;
using System.Text.Json;

namespace Ciir.CSharp.Tests.Integration;

/// <summary>
/// Exercises the real analysis pipeline (<see cref="CompilationAnalyzer"/>) against the
/// <c>fixtures/BasicSolution</c> sample project. A <see cref="CSharpCompilation"/> is built
/// directly from the fixture's source files and the current runtime's reference assemblies,
/// rather than via <c>MSBuildWorkspace</c>: this keeps the test focused on the semantic analysis
/// logic itself (type/member discovery, relation/condition extraction, embeddingText), which is
/// exactly what <see cref="CSharpCodeAnalyzer"/> delegates to after loading a project.
/// </summary>
public class BasicSolutionIntegrationTests
{
    [Fact]
    public void Analyze_ProducesSchemaValidDocuments_ForBasicSolutionFixture()
    {
        var documents = AnalyzeFixture();

        documents.ShouldNotBeEmpty();
        AssertAllDocumentsAreSchemaValid(documents);
        AssertTopLevelEntitiesArePresent(documents);
        AssertAuthorizeAsyncIsCorrect(documents);
    }

    [Fact]
    public void Analyze_IsDeterministic_AcrossRepeatedRuns()
    {
        var first = AnalyzeFixture();
        var second = AnalyzeFixture();

        var jsonOptions = CiirJsonSerializerOptions.Create();
        var firstJson = first.Select(d => JsonSerializer.Serialize(d, jsonOptions)).ToArray();
        var secondJson = second.Select(d => JsonSerializer.Serialize(d, jsonOptions)).ToArray();

        firstJson.ShouldBe(secondJson);
    }

    private static List<CiirDocument> AnalyzeFixture()
    {
        var fixtureDirectory = FindFixturePath("BasicSolution");
        var sourceFiles = Directory.EnumerateFiles(fixtureDirectory, "*.cs").OrderBy(path => path, StringComparer.Ordinal);

        var syntaxTrees = sourceFiles
            .Select(path => CSharpSyntaxTree.ParseText(File.ReadAllText(path), path: path))
            .Append(ImplicitUsingsSyntaxTree())
            .ToArray();

        var trustedAssemblies = ((string)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES")!)
            .Split(Path.PathSeparator)
            .Select(path => (MetadataReference)MetadataReference.CreateFromFile(path));

        var compilation = CSharpCompilation.Create(
            "BasicSolution",
            syntaxTrees,
            trustedAssemblies,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary, nullableContextOptions: NullableContextOptions.Enable));

        var options = new AnalysisOptions { OutputPath = Path.GetTempPath() };
        var context = new RelationResolutionContext
        {
            CurrentAssemblyName = "BasicSolution",
            CurrentProjectName = "BasicSolution",
            AssemblyNameToProjectName = new Dictionary<string, string> { ["BasicSolution"] = "BasicSolution" },
        };

        return [.. CompilationAnalyzer.Analyze(compilation, "BasicSolution", fixtureDirectory, context, options, TestContext.Current.CancellationToken)];
    }

    private static void AssertAllDocumentsAreSchemaValid(IEnumerable<CiirDocument> documents)
    {
        var jsonOptions = CiirJsonSerializerOptions.Create();

        foreach (var document in documents)
        {
            var json = JsonSerializer.Serialize(document, jsonOptions);
            var instance = JsonDocument.Parse(json).RootElement;
            var results = CiirSchemaFixture.Schema.Evaluate(instance, new EvaluationOptions { OutputFormat = OutputFormat.List });

            results.IsValid.ShouldBeTrue($"{document.Kind} '{document.Symbol.QualifiedName}' failed schema validation:\n{json}");
        }
    }

    private static void AssertTopLevelEntitiesArePresent(List<CiirDocument> documents)
    {
        documents.ShouldContain(d => d.Kind == CiirKind.Project && string.Equals(d.Symbol.Name, "BasicSolution", StringComparison.Ordinal));
        documents.ShouldContain(d => d.Kind == CiirKind.Namespace && string.Equals(d.Symbol.QualifiedName, "Payments.Domain", StringComparison.Ordinal));
        documents.ShouldContain(d => d.Kind == CiirKind.Namespace && string.Equals(d.Symbol.QualifiedName, "Payments.Application", StringComparison.Ordinal));
        documents.ShouldContain(d => d.Kind == CiirKind.Type && string.Equals(d.Symbol.QualifiedName, "Payments.Domain.Order", StringComparison.Ordinal));
        documents.ShouldContain(d => d.Kind == CiirKind.Type && string.Equals(d.Symbol.QualifiedName, "Payments.Domain.IPaymentGateway", StringComparison.Ordinal));
        documents.ShouldContain(d => d.Kind == CiirKind.Property && string.Equals(d.Symbol.QualifiedName, "Payments.Domain.Order.Total", StringComparison.Ordinal));
        documents.ShouldContain(d => d.Kind == CiirKind.Constructor && string.Equals(d.Symbol.Container, "Payments.Application.PaymentService", StringComparison.Ordinal));

        var order = documents.Single(d => d.Kind == CiirKind.Type && string.Equals(d.Symbol.QualifiedName, "Payments.Domain.Order", StringComparison.Ordinal));
        order.Type.ShouldNotBeNull();
        order.Type.TypeKind.ShouldBe(Core.Symbols.CiirTypeKind.Class);

        var gateway = documents.Single(d => d.Kind == CiirKind.Type && string.Equals(d.Symbol.QualifiedName, "Payments.Domain.IPaymentGateway", StringComparison.Ordinal));
        gateway.Type.ShouldNotBeNull();
        gateway.Type.TypeKind.ShouldBe(Core.Symbols.CiirTypeKind.Interface);
    }

    private static void AssertAuthorizeAsyncIsCorrect(List<CiirDocument> documents)
    {
        var authorizeMethod = documents.Single(d =>
            d.Kind == CiirKind.Method &&
            string.Equals(d.Symbol.Name, "AuthorizeAsync", StringComparison.Ordinal) &&
            string.Equals(d.Symbol.Container, "Payments.Application.PaymentService", StringComparison.Ordinal));

        authorizeMethod.Documentation.ShouldNotBeNull();
        authorizeMethod.Documentation.Summary.ShouldBe("Authorizes a payment for the given order.");
        authorizeMethod.Method.ShouldNotBeNull();
        authorizeMethod.Method.EmbeddingReturnType.ShouldBe("Payments.Domain.PaymentResult");

        authorizeMethod.Relations.ShouldContain(r =>
            r.Kind == CiirRelationKind.Calls &&
            r.Target.Symbol.StartsWith("Payments.Domain.IPaymentGateway.AuthorizeAsync", StringComparison.Ordinal) &&
            r.Resolution.Status == CiirResolutionStatus.Resolved);

        authorizeMethod.Relations.ShouldContain(r =>
            r.Kind == CiirRelationKind.Throws &&
            string.Equals(r.Target.Symbol, "Payments.Domain.InvalidOrderException", StringComparison.Ordinal));

        authorizeMethod.Relations.ShouldContain(r =>
            r.Kind == CiirRelationKind.Reads &&
            string.Equals(r.Target.Symbol, "Payments.Domain.Order.Total", StringComparison.Ordinal));

        authorizeMethod.Conditions.ShouldContain(c =>
            c.Kind == CiirConditionKind.Guard && string.Equals(c.Expression, "order.Total <= 0", StringComparison.Ordinal));

        var orderTotalProperty = documents.Single(d =>
            d.Kind == CiirKind.Property && string.Equals(d.Symbol.QualifiedName, "Payments.Domain.Order.Total", StringComparison.Ordinal));

        authorizeMethod.Relations.ShouldContain(r =>
            r.Kind == CiirRelationKind.Reads &&
            string.Equals(r.Target.Symbol, "Payments.Domain.Order.Total", StringComparison.Ordinal) &&
            r.Resolution.Status == CiirResolutionStatus.Resolved &&
            r.Resolution.Origin == CiirResolutionOrigin.Project &&
            r.Target.Id == orderTotalProperty.Id);
    }

    private static SyntaxTree ImplicitUsingsSyntaxTree() => CSharpSyntaxTree.ParseText(
        """
        global using global::System;
        global using global::System.Collections.Generic;
        global using global::System.IO;
        global using global::System.Linq;
        global using global::System.Threading;
        global using global::System.Threading.Tasks;
        """,
        path: "GlobalUsings.g.cs");

    private static string FindFixturePath(string relativePath)
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !Directory.Exists(Path.Combine(directory.FullName, "fixtures")))
        {
            directory = directory.Parent;
        }

        if (directory is null)
        {
            throw new InvalidOperationException("Could not locate the repository's fixtures directory from the test output.");
        }

        return Path.Combine(directory.FullName, "fixtures", relativePath);
    }
}
