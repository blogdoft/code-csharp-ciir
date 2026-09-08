namespace Ciir.Application.Ports;

/// <summary>
/// Lists the C# project paths referenced by a solution file. Implemented in the C# analyzer
/// adapter, since only it may open a solution with Roslyn/MSBuild tooling.
/// </summary>
public interface ISolutionProjectLister
{
    /// <summary>Lists the full paths of the C# projects referenced by <paramref name="solutionPath"/>.</summary>
    /// <param name="solutionPath">The full path to a <c>.sln</c> or <c>.slnx</c> file.</param>
    /// <param name="cancellationToken">A token to observe for cancellation.</param>
    Task<IReadOnlyList<string>> ListProjectPathsAsync(string solutionPath, CancellationToken cancellationToken);
}
