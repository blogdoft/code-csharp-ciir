using Ciir.Application.Model;
using Ciir.Core;
using Ciir.Core.Relations;
using Ciir.CSharp.Workspace;
using Shouldly;
using System.Diagnostics;

namespace Ciir.CSharp.Tests.Integration;

/// <summary>
/// Regression test for <see cref="CompilationBuilder"/>'s project-reference resolution:
/// <c>Project.MetadataReferences</c> only ever contains "external" references (NuGet/BCL/direct
/// file references) — in-solution project-to-project references live on
/// <c>Project.ProjectReferences</c>. A relation whose target belongs to a different project
/// analyzed in this same run (reachable via <c>Project.ProjectReferences</c>, directly or
/// transitively) must resolve as <c>status: "resolved"</c> / <c>origin: "solution"</c> with a
/// populated <c>target.id</c> — not as an external dependency, and not as unresolved.
/// </summary>
public class CrossProjectReferenceIntegrationTests
{
    [Fact]
    public async Task AnalyzeAsync_ResolvesTypesFromReferencedProject_AsResolvedSolution_NotExternal()
    {
        var rootDirectory = FindFixturePath("MultipleProjects");
        var applicationPath = Path.Combine(rootDirectory, "Application", "Application.csproj");
        await RestoreAsync(applicationPath, TestContext.Current.CancellationToken);

        var analyzer = new CSharpCodeAnalyzer();
        var options = new AnalysisOptions { OutputPath = Path.GetTempPath() };

        var documents = new List<CiirDocument>();
        await foreach (var document in analyzer.AnalyzeAsync(applicationPath, rootDirectory, options, TestContext.Current.CancellationToken))
        {
            documents.Add(document);
        }

        var applyDiscount = documents.Single(d =>
            d.Kind == CiirKind.Method &&
            string.Equals(d.Symbol.Name, "ApplyDiscount", StringComparison.Ordinal));

        // The root is the shared "MultipleProjects" parent, not the Application project's own
        // folder — so source.path must include the "Application/" segment (root-relative), not
        // just the file's name relative to its own project (which would be "PriceCalculator.cs").
        applyDiscount.Source.ShouldNotBeNull().Path.ShouldStartWith("Application/");

        applyDiscount.Relations.ShouldContain(r =>
            r.Kind == CiirRelationKind.Constructs &&
            string.Equals(r.Target.Symbol, "MultipleProjects.Domain.Money", StringComparison.Ordinal) &&
            r.Resolution.Status == CiirResolutionStatus.Resolved &&
            r.Resolution.Origin == CiirResolutionOrigin.Solution &&
            r.Target.Id != null);

        // Money.Amount is a positional record property — it has no PropertyDeclarationSyntax, so
        // no CIIR document (and therefore no id) is ever emitted for it, even though it resolves.
        applyDiscount.Relations.ShouldContain(r =>
            r.Kind == CiirRelationKind.Reads &&
            string.Equals(r.Target.Symbol, "MultipleProjects.Domain.Money.Amount", StringComparison.Ordinal) &&
            r.Resolution.Status == CiirResolutionStatus.Resolved &&
            r.Resolution.Origin == CiirResolutionOrigin.Solution &&
            r.Target.Id == null);

        applyDiscount.Relations.ShouldNotContain(r => r.Resolution.Status == CiirResolutionStatus.Unresolved);
        applyDiscount.Relations.ShouldNotContain(r => r.Resolution.Status == CiirResolutionStatus.External);
    }

    [Fact]
    public async Task AnalyzeAsync_TargetId_IsJoinableAcrossSeparatelyAnalyzedProjects()
    {
        var rootDirectory = FindFixturePath("MultipleProjects");
        var applicationPath = Path.Combine(rootDirectory, "Application", "Application.csproj");
        var domainPath = Path.Combine(rootDirectory, "Domain", "Domain.csproj");
        await RestoreAsync(applicationPath, TestContext.Current.CancellationToken);
        await RestoreAsync(domainPath, TestContext.Current.CancellationToken);

        var analyzer = new CSharpCodeAnalyzer();
        var options = new AnalysisOptions { OutputPath = Path.GetTempPath() };

        var applicationDocuments = new List<CiirDocument>();
        await foreach (var document in analyzer.AnalyzeAsync(applicationPath, rootDirectory, options, TestContext.Current.CancellationToken))
        {
            applicationDocuments.Add(document);
        }

        var domainDocuments = new List<CiirDocument>();
        await foreach (var document in analyzer.AnalyzeAsync(domainPath, rootDirectory, options, TestContext.Current.CancellationToken))
        {
            domainDocuments.Add(document);
        }

        var applyDiscount = applicationDocuments.Single(d =>
            d.Kind == CiirKind.Method &&
            string.Equals(d.Symbol.Name, "ApplyDiscount", StringComparison.Ordinal));

        var constructsMoney = applyDiscount.Relations.Single(r =>
            r.Kind == CiirRelationKind.Constructs &&
            string.Equals(r.Target.Symbol, "MultipleProjects.Domain.Money", StringComparison.Ordinal));

        var moneyDocument = domainDocuments.Single(d =>
            d.Kind == CiirKind.Type && string.Equals(d.Symbol.QualifiedName, "MultipleProjects.Domain.Money", StringComparison.Ordinal));

        constructsMoney.Target.Id.ShouldBe(moneyDocument.Id);
    }

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

    private static async Task RestoreAsync(string projectPath, CancellationToken cancellationToken)
    {
        using var process = Process.Start(new ProcessStartInfo("dotnet", ["restore", projectPath])
        {
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
        }) ?? throw new InvalidOperationException("Failed to start 'dotnet restore'.");

        await process.WaitForExitAsync(cancellationToken);
        if (process.ExitCode != 0)
        {
            var error = await process.StandardError.ReadToEndAsync(cancellationToken);
            throw new InvalidOperationException($"'dotnet restore' failed for '{projectPath}': {error}");
        }
    }
}
