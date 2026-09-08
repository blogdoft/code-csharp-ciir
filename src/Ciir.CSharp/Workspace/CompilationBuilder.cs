using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace Ciir.CSharp.Workspace;

/// <summary>
/// Builds the <see cref="Compilation"/> for a workspace-loaded <see cref="Project"/>, working
/// around MSBuild toolset/workspace combinations where <see cref="Project.GetCompilationAsync"/>
/// returns <see langword="null"/> because the design-time build never captured the project's
/// <see cref="Project.ParseOptions"/>/<see cref="Project.CompilationOptions"/> — even though
/// <see cref="Project.MetadataReferences"/> resolved correctly, since that is a separate
/// mechanism. In that situation, <see cref="Microsoft.CodeAnalysis.Document.GetSyntaxTreeAsync"/>
/// also returns <see langword="null"/> for every document (it needs the same missing
/// <see cref="Project.ParseOptions"/> to parse the text), so the fallback re-parses each
/// document's raw source text directly instead.
/// </summary>
/// <remarks>
/// <see cref="Project.MetadataReferences"/> only ever contains "external" references (NuGet
/// packages, the BCL, direct file references) — in-solution project-to-project references live
/// separately on <see cref="Project.ProjectReferences"/> and are normally resolved by
/// <see cref="Project.GetCompilationAsync"/> into <see cref="CompilationReference"/>s
/// automatically. The fallback must do this itself, recursively (a referenced project may need
/// the very same fallback), with memoization so a project referenced by several others is only
/// built once.
/// </remarks>
internal static class CompilationBuilder
{
    public static Task<Compilation> BuildAsync(Project project, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(project);

        return BuildAsync(project, new Dictionary<ProjectId, Task<Compilation>>(), cancellationToken);
    }

    private static Task<Compilation> BuildAsync(Project project, Dictionary<ProjectId, Task<Compilation>> cache, CancellationToken cancellationToken)
    {
        if (cache.TryGetValue(project.Id, out var cached))
        {
            return cached;
        }

        var task = BuildCoreAsync(project, cache, cancellationToken);
        cache[project.Id] = task;
        return task;
    }

    private static async Task<Compilation> BuildCoreAsync(Project project, Dictionary<ProjectId, Task<Compilation>> cache, CancellationToken cancellationToken)
    {
        var compilation = await project.GetCompilationAsync(cancellationToken).ConfigureAwait(false);
        if (compilation is not null)
        {
            return compilation;
        }

        var parseOptions = project.ParseOptions as CSharpParseOptions ?? new CSharpParseOptions(LanguageVersion.Latest);
        var compilationOptions = project.CompilationOptions as CSharpCompilationOptions
            ?? new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary, nullableContextOptions: NullableContextOptions.Enable);

        var syntaxTrees = new List<SyntaxTree>();
        foreach (var document in project.Documents)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var text = await document.GetTextAsync(cancellationToken).ConfigureAwait(false);
            syntaxTrees.Add(CSharpSyntaxTree.ParseText(text, parseOptions, path: document.FilePath ?? document.Name, cancellationToken: cancellationToken));
        }

        var references = new List<MetadataReference>(project.MetadataReferences);
        foreach (var projectReference in project.ProjectReferences)
        {
            var referencedProject = project.Solution.GetProject(projectReference.ProjectId);
            if (referencedProject is null)
            {
                continue;
            }

            var referencedCompilation = await BuildAsync(referencedProject, cache, cancellationToken).ConfigureAwait(false);
            references.Add(referencedCompilation.ToMetadataReference());
        }

        return CSharpCompilation.Create(project.Name, syntaxTrees, references, compilationOptions);
    }
}
