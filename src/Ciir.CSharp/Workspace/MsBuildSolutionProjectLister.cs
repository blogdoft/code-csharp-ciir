using Ciir.Application.Ports;
using Ciir.CSharp.Bootstrap;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.MSBuild;

namespace Ciir.CSharp.Workspace;

/// <summary>Lists the C# project paths referenced by a solution, using <see cref="MSBuildWorkspace"/>.</summary>
public sealed class MsBuildSolutionProjectLister : ISolutionProjectLister
{
    /// <inheritdoc />
    public async Task<IReadOnlyList<string>> ListProjectPathsAsync(string solutionPath, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(solutionPath);

        MsBuildEnvironment.EnsureRegistered();

        using var workspace = MSBuildWorkspace.Create();
        var solution = await workspace.OpenSolutionAsync(solutionPath, cancellationToken: cancellationToken);

        return [.. solution.Projects
            .Where(project => string.Equals(project.Language, LanguageNames.CSharp, StringComparison.Ordinal) && project.FilePath is not null)
            .Select(project => project.FilePath!)];
    }
}
