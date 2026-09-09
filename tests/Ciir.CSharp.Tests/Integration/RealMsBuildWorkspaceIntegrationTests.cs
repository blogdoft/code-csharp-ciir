using Ciir.Application.Model;
using Ciir.Core;
using Ciir.Core.Relations;
using Ciir.CSharp.Workspace;
using Shouldly;
using System.Diagnostics;

namespace Ciir.CSharp.Tests.Integration;

/// <summary>
/// Exercises <see cref="CSharpCodeAnalyzer"/> through the real <c>MSBuildWorkspace</c> path (as
/// opposed to <see cref="BasicSolutionIntegrationTests"/>, which targets
/// <see cref="CompilationAnalyzer"/> directly against a hand-built <see cref="Microsoft.CodeAnalysis.Compilation"/>).
/// This is the code path the CLI actually uses, so it is what guards against a regression of
/// <see cref="CompilationBuilder"/>'s fallback for MSBuild toolsets that fail to populate
/// <c>Project.ParseOptions</c>/<c>Project.CompilationOptions</c> during the design-time build.
/// </summary>
public class RealMsBuildWorkspaceIntegrationTests
{
    [Fact]
    public async Task AnalyzeAsync_ProducesRealDocuments_ThroughMsBuildWorkspace()
    {
        var projectPath = FindFixturePath(Path.Combine("BasicSolution", "BasicSolution.csproj"));
        await RestoreAsync(projectPath, TestContext.Current.CancellationToken);

        var analyzer = new CSharpCodeAnalyzer();
        var rootDirectory = Path.GetDirectoryName(projectPath)!;
        var options = new AnalysisOptions { OutputPath = Path.GetTempPath() };

        var documents = new List<CiirDocument>();
        await foreach (var document in analyzer.AnalyzeAsync(projectPath, rootDirectory, options, TestContext.Current.CancellationToken))
        {
            documents.Add(document);
        }

        documents.ShouldNotBeEmpty();
        documents.ShouldContain(d => d.Kind == CiirKind.Type && string.Equals(d.Symbol.QualifiedName, "Payments.Domain.Order", StringComparison.Ordinal));

        var order = documents.Single(d => d.Kind == CiirKind.Type && string.Equals(d.Symbol.QualifiedName, "Payments.Domain.Order", StringComparison.Ordinal));
        order.Source.ShouldNotBeNull().Path.ShouldBe("Order.cs");

        var authorizeMethod = documents.Single(d =>
            d.Kind == CiirKind.Method &&
            string.Equals(d.Symbol.Name, "AuthorizeAsync", StringComparison.Ordinal) &&
            string.Equals(d.Symbol.Container, "Payments.Application.PaymentService", StringComparison.Ordinal));

        authorizeMethod.Relations.ShouldContain(r =>
            r.Kind == CiirRelationKind.Calls &&
            r.Target.Symbol.StartsWith("Payments.Domain.IPaymentGateway.AuthorizeAsync", StringComparison.Ordinal) &&
            r.Resolution.Status == CiirResolutionStatus.Resolved);
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
