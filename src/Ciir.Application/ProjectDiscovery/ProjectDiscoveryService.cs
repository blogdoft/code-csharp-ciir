using Ciir.Application.Model;
using Ciir.Application.Ports;

namespace Ciir.Application.ProjectDiscovery;

/// <summary>
/// Expands a resolved <see cref="AnalysisInput"/> into the unique set of project paths to
/// analyze. A project referenced by a discovered solution is never analyzed a second time just
/// because its <c>.csproj</c> was also found while scanning a directory.
/// </summary>
public sealed class ProjectDiscoveryService(ISolutionProjectLister solutionProjectLister)
{
    /// <summary>
    /// Discovers every file under <paramref name="rootDirectory"/> that is neither a project or
    /// solution file (handled by <see cref="DiscoverAsync"/>) nor a C# source file, skipping the
    /// same ignored directories. Always rooted at the analysis root, regardless of input type.
    /// </summary>
    /// <param name="rootDirectory">The analysis root to walk.</param>
    public static IReadOnlyList<string> DiscoverAuxiliaryFiles(string rootDirectory)
    {
        var directories = new Stack<string>();
        directories.Push(rootDirectory);
        var files = new List<string>();

        while (directories.Count > 0)
        {
            var currentDirectory = directories.Pop();

            foreach (var file in Directory.EnumerateFiles(currentDirectory))
            {
                if (!IsProjectOrSolutionFile(file) && !string.Equals(Path.GetExtension(file), ".cs", StringComparison.OrdinalIgnoreCase))
                {
                    files.Add(Path.GetFullPath(file));
                }
            }

            foreach (var subdirectory in Directory.EnumerateDirectories(currentDirectory))
            {
                if (!IgnoredDirectories.IsIgnored(Path.GetFileName(subdirectory)))
                {
                    directories.Push(subdirectory);
                }
            }
        }

        return [.. new SortedSet<string>(files, StringComparer.OrdinalIgnoreCase)];
    }

    /// <summary>Discovers the unique, deterministically ordered set of project paths for <paramref name="input"/>.</summary>
    /// <param name="input">The resolved analysis input.</param>
    /// <param name="cancellationToken">A token to observe for cancellation.</param>
    public async Task<IReadOnlyList<string>> DiscoverAsync(AnalysisInput input, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(input);

        var projectPaths = input.Type switch
        {
            AnalysisInputType.Project => [Path.GetFullPath(input.Path)],
            AnalysisInputType.Solution => await ListSolutionProjectsAsync(input.Path, cancellationToken),
            AnalysisInputType.Directory => await DiscoverInDirectoryAsync(input.Path, cancellationToken),
            _ => throw new ArgumentOutOfRangeException(nameof(input), input.Type, message: null),
        };

        return [.. new SortedSet<string>(projectPaths, StringComparer.OrdinalIgnoreCase)];
    }

    private static IEnumerable<string> EnumerateProjectFiles(string rootDirectory)
    {
        var directories = new Stack<string>();
        directories.Push(rootDirectory);

        while (directories.Count > 0)
        {
            var currentDirectory = directories.Pop();

            foreach (var file in Directory.EnumerateFiles(currentDirectory))
            {
                if (IsProjectOrSolutionFile(file))
                {
                    yield return file;
                }
            }

            foreach (var subdirectory in Directory.EnumerateDirectories(currentDirectory))
            {
                if (!IgnoredDirectories.IsIgnored(Path.GetFileName(subdirectory)))
                {
                    directories.Push(subdirectory);
                }
            }
        }
    }

    private static bool IsProjectOrSolutionFile(string path)
    {
        var extension = Path.GetExtension(path);
        return string.Equals(extension, ".sln", StringComparison.OrdinalIgnoreCase)
            || string.Equals(extension, ".slnx", StringComparison.OrdinalIgnoreCase)
            || string.Equals(extension, ".csproj", StringComparison.OrdinalIgnoreCase);
    }

    private async Task<HashSet<string>> ListSolutionProjectsAsync(string solutionPath, CancellationToken cancellationToken)
    {
        var projects = await solutionProjectLister.ListProjectPathsAsync(solutionPath, cancellationToken);
        return new HashSet<string>(projects.Select(Path.GetFullPath), StringComparer.OrdinalIgnoreCase);
    }

    private async Task<HashSet<string>> DiscoverInDirectoryAsync(string rootDirectory, CancellationToken cancellationToken)
    {
        var result = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var file in EnumerateProjectFiles(rootDirectory))
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (string.Equals(Path.GetExtension(file), ".csproj", StringComparison.OrdinalIgnoreCase))
            {
                result.Add(Path.GetFullPath(file));
                continue;
            }

            var projectsInSolution = await solutionProjectLister.ListProjectPathsAsync(file, cancellationToken);
            foreach (var projectPath in projectsInSolution)
            {
                result.Add(Path.GetFullPath(projectPath));
            }
        }

        return result;
    }
}
