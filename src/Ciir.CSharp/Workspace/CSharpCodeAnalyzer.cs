using Ciir.Application.Model;
using Ciir.Application.Ports;
using Ciir.Core;
using Ciir.CSharp.Bootstrap;
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
        AnalysisOptions options,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(projectPath);
        ArgumentNullException.ThrowIfNull(options);

        MsBuildEnvironment.EnsureRegistered();

        using var workspace = MSBuildWorkspace.Create();
        var project = await workspace.OpenProjectAsync(projectPath, cancellationToken: cancellationToken);
        var compilation = await CompilationBuilder.BuildAsync(project, cancellationToken);

        var projectDirectory = Path.GetDirectoryName(Path.GetFullPath(projectPath))!;

        foreach (var document in CompilationAnalyzer.Analyze(compilation, project.Name, projectDirectory, options, cancellationToken))
        {
            yield return document;
        }
    }
}
