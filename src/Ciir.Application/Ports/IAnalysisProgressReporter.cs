namespace Ciir.Application.Ports;

/// <summary>
/// Reports analysis progress to the caller (e.g. the CLI's console output), independent of the
/// analysis logic itself. A no-op implementation is always a valid choice (e.g. <c>--no-progress</c>).
/// </summary>
public interface IAnalysisProgressReporter
{
    /// <summary>Called once project discovery completes.</summary>
    /// <param name="projectCount">The number of unique projects discovered.</param>
    void OnProjectsDiscovered(int projectCount);

    /// <summary>Called when a project's analysis begins.</summary>
    /// <param name="projectPath">The project's full path.</param>
    /// <param name="index">The 1-based position of this project among all discovered projects.</param>
    /// <param name="total">The total number of discovered projects.</param>
    void OnProjectStarted(string projectPath, int index, int total);

    /// <summary>Called when a project's analysis completes successfully.</summary>
    /// <param name="projectPath">The project's full path.</param>
    /// <param name="documentCount">The number of documents produced for this project.</param>
    void OnProjectCompleted(string projectPath, int documentCount);

    /// <summary>Called when a project's analysis fails.</summary>
    /// <param name="projectPath">The project's full path.</param>
    /// <param name="message">A human-readable description of the failure.</param>
    void OnProjectFailed(string projectPath, string message);
}
