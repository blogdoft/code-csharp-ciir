using Ciir.Application.Model;
using Ciir.Core;
using Ciir.Core.Relations;
using Ciir.CSharp.Workspace;
using Shouldly;
using System.Diagnostics;

namespace Ciir.CSharp.Tests.Integration;

/// <summary>
/// Regression test for <see cref="CompilationBuilder"/>'s project-reference fallback:
/// <c>Project.MetadataReferences</c> only ever contains "external" references (NuGet/BCL/direct
/// file references) — in-solution project-to-project references live separately on
/// <c>Project.ProjectReferences</c>. Before this was handled, every relation pointing at a type
/// declared in a referenced project (a very common shape in any multi-project solution) came back
/// with <c>resolution.status: "unresolved"</c> instead of <c>"external"</c>/<c>"dependency"</c>.
/// </summary>
public class CrossProjectReferenceIntegrationTests
{
    [Fact]
    public async Task AnalyzeAsync_ResolvesTypesFromReferencedProject_AsExternalDependency_NotUnresolved()
    {
        var projectPath = FindFixturePath(Path.Combine("MultipleProjects", "Application", "Application.csproj"));
        await RestoreAsync(projectPath, TestContext.Current.CancellationToken);

        var analyzer = new CSharpCodeAnalyzer();
        var options = new AnalysisOptions { OutputPath = Path.GetTempPath() };

        var documents = new List<CiirDocument>();
        await foreach (var document in analyzer.AnalyzeAsync(projectPath, options, TestContext.Current.CancellationToken))
        {
            documents.Add(document);
        }

        var applyDiscount = documents.Single(d =>
            d.Kind == CiirKind.Method &&
            string.Equals(d.Symbol.Name, "ApplyDiscount", StringComparison.Ordinal));

        applyDiscount.Relations.ShouldContain(r =>
            r.Kind == CiirRelationKind.Constructs &&
            string.Equals(r.Target.Symbol, "MultipleProjects.Domain.Money", StringComparison.Ordinal) &&
            r.Resolution.Status == CiirResolutionStatus.External &&
            r.Resolution.Origin == CiirResolutionOrigin.Dependency);

        applyDiscount.Relations.ShouldContain(r =>
            r.Kind == CiirRelationKind.Reads &&
            string.Equals(r.Target.Symbol, "MultipleProjects.Domain.Money.Amount", StringComparison.Ordinal) &&
            r.Resolution.Status == CiirResolutionStatus.External);

        applyDiscount.Relations.ShouldNotContain(r => r.Resolution.Status == CiirResolutionStatus.Unresolved);
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
