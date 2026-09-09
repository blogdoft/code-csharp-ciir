using Ciir.Application.Model;
using Ciir.Core;

namespace Ciir.Application.Ports;

/// <summary>
/// Analyzes a single project and streams the CIIR documents it produces. Implemented once per
/// source language (the C# generator is the only implementation today); Application depends only
/// on this abstraction and never on Roslyn types.
/// </summary>
public interface ICodeAnalyzer
{
    /// <summary>Whether this analyzer can process <paramref name="projectPath"/>.</summary>
    /// <param name="projectPath">The full path to a project file.</param>
    bool CanAnalyze(string projectPath);

    /// <summary>
    /// Analyzes the project at <paramref name="projectPath"/>, yielding each <see cref="CiirDocument"/>
    /// as soon as it is produced rather than buffering the full result in memory.
    /// </summary>
    /// <param name="projectPath">The full path to the project file to analyze.</param>
    /// <param name="rootDirectory">
    /// The directory every emitted <c>source.path</c> is computed relative to — the analysis
    /// root the caller originally resolved the input to, not necessarily this project's own
    /// directory (e.g. when analyzing a directory or solution containing several projects).
    /// </param>
    /// <param name="options">Analysis options (e.g. whether to include source text).</param>
    /// <param name="cancellationToken">A token to observe for cancellation.</param>
    IAsyncEnumerable<CiirDocument> AnalyzeAsync(string projectPath, string rootDirectory, AnalysisOptions options, CancellationToken cancellationToken);
}
