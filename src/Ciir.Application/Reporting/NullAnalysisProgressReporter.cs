using Ciir.Application.Ports;

namespace Ciir.Application.Reporting;

/// <summary>A no-op <see cref="IAnalysisProgressReporter"/>, used when progress reporting is suppressed.</summary>
public sealed class NullAnalysisProgressReporter : IAnalysisProgressReporter
{
    /// <summary>The shared singleton instance.</summary>
    public static readonly NullAnalysisProgressReporter Instance = new();

    private NullAnalysisProgressReporter()
    {
    }

    /// <inheritdoc />
    public void OnProjectsDiscovered(int projectCount)
    {
    }

    /// <inheritdoc />
    public void OnProjectStarted(string projectPath, int index, int total)
    {
    }

    /// <inheritdoc />
    public void OnProjectCompleted(string projectPath, int documentCount)
    {
    }

    /// <inheritdoc />
    public void OnProjectFailed(string projectPath, string message)
    {
    }
}
