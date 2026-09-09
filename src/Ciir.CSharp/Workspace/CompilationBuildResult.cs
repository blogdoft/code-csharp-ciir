using Microsoft.CodeAnalysis;

namespace Ciir.CSharp.Workspace;

/// <summary>
/// The <see cref="Compilation"/> built for a project, plus a lookup from every other assembly
/// name reachable in the project's <see cref="Project.Solution"/> to its Roslyn project name.
/// </summary>
/// <param name="Compilation">The built compilation.</param>
/// <param name="AssemblyNameToProjectName">Maps every reachable assembly name to its Roslyn project name.</param>
internal readonly record struct CompilationBuildResult(Compilation Compilation, IReadOnlyDictionary<string, string> AssemblyNameToProjectName);
