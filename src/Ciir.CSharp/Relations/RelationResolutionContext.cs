namespace Ciir.CSharp.Relations;

/// <summary>
/// What <see cref="RelationResolutionClassifier"/> and the relation extractors need to classify a
/// relation target's origin and, when resolved, compute its <see cref="Ciir.Core.Relations.CiirRelationTarget.Id"/>:
/// the project currently being analyzed (its compiled assembly name and Roslyn project name), plus
/// a lookup from every other assembly name reachable in this analysis pass — i.e. loaded into the
/// current project's <see cref="Microsoft.CodeAnalysis.Solution"/>, directly or transitively via
/// <see cref="Microsoft.CodeAnalysis.ProjectReference"/> — to that project's Roslyn project name.
/// </summary>
internal sealed class RelationResolutionContext
{
    /// <summary>The compiled assembly name of the project currently being analyzed.</summary>
    public required string CurrentAssemblyName { get; init; }

    /// <summary>The Roslyn project name (matches the <c>.csproj</c> file stem) of the project currently being analyzed.</summary>
    public required string CurrentProjectName { get; init; }

    /// <summary>Maps every other assembly name reachable in this analysis pass to its Roslyn project name.</summary>
    public required IReadOnlyDictionary<string, string> AssemblyNameToProjectName { get; init; }

    /// <summary>Looks up the Roslyn project name that produced <paramref name="assemblyName"/>, when known.</summary>
    /// <param name="assemblyName">The assembly name to look up.</param>
    /// <param name="projectName">The matching Roslyn project name, when found.</param>
    public bool TryGetProjectName(string assemblyName, out string projectName) =>
        AssemblyNameToProjectName.TryGetValue(assemblyName, out projectName!);
}
