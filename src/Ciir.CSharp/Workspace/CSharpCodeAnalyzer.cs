using Ciir.Application.Model;
using Ciir.Application.Ports;
using Ciir.Core;
using Ciir.CSharp.Bootstrap;
using Ciir.CSharp.Relations;
using Microsoft.CodeAnalysis.MSBuild;
using System.Runtime.CompilerServices;

namespace Ciir.CSharp.Workspace;

/// <summary>The C#/Roslyn implementation of <see cref="ICodeAnalyzer"/>.</summary>
public sealed class CSharpCodeAnalyzer : ICodeAnalyzer
{
    /// <inheritdoc />
    public bool CanAnalyze(string projectPath) =>
        string.Equals(Path.GetExtension(projectPath), ".csproj", StringComparison.OrdinalIgnoreCase);

    /// <inheritdoc />
    public async IAsyncEnumerable<CiirDocument> AnalyzeAsync(
        string projectPath,
        string rootDirectory,
        AnalysisOptions options,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(projectPath);
        ArgumentNullException.ThrowIfNull(rootDirectory);
        ArgumentNullException.ThrowIfNull(options);

        MsBuildEnvironment.EnsureRegistered();

        using var workspace = MSBuildWorkspace.Create();
        var project = await workspace.OpenProjectAsync(projectPath, cancellationToken: cancellationToken);
        var buildResult = await CompilationBuilder.BuildAsync(project, cancellationToken);

        var context = new RelationResolutionContext
        {
            CurrentAssemblyName = buildResult.Compilation.AssemblyName ?? project.Name,
            CurrentProjectName = project.Name,
            AssemblyNameToProjectName = buildResult.AssemblyNameToProjectName,
        };

        foreach (var document in CompilationAnalyzer.Analyze(buildResult.Compilation, project.Name, rootDirectory, context, options, cancellationToken))
        {
            yield return document;
        }
    }
}
