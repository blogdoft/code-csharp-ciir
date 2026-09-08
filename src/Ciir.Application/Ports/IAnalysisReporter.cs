using Ciir.Application.Model;
using Ciir.Core;

namespace Ciir.Application.Ports;

/// <summary>
/// Accumulates operational statistics about an analysis run as it streams, so
/// <c>analysis-report.json</c>/<c>manifest.json</c> can be produced without a second pass over
/// the data or retaining every document in memory.
/// </summary>
public interface IAnalysisReporter
{
    /// <summary>Records that a project was discovered and will be analyzed.</summary>
    /// <param name="projectPath">The project's full path.</param>
    void RecordProjectDiscovered(string projectPath);

    /// <summary>Records that a project finished analyzing successfully.</summary>
    /// <param name="projectPath">The project's full path.</param>
    void RecordProjectAnalyzed(string projectPath);

    /// <summary>Records that a project failed to load or analyze.</summary>
    /// <param name="projectPath">The project's full path.</param>
    /// <param name="message">A human-readable description of the failure.</param>
    /// <param name="category">A short machine-readable category, e.g. <c>"project_load"</c>.</param>
    void RecordProjectFailed(string projectPath, string message, string category);

    /// <summary>Records a document that was written to the CIIR output.</summary>
    /// <param name="document">The document that was written.</param>
    void RecordDocument(CiirDocument document);

    /// <summary>Builds the final report from everything recorded so far.</summary>
    AnalysisReport BuildReport();
}
