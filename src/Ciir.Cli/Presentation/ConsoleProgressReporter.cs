using Ciir.Application.Ports;

namespace Ciir.Cli.Presentation;

/// <summary>Reports analysis progress to the console. Console output lives in this presentation layer, alongside <see cref="SplashScreen"/>.</summary>
internal sealed class ConsoleProgressReporter : IAnalysisProgressReporter
{
    /// <inheritdoc />
    public void OnProjectsDiscovered(int projectCount)
    {
        Console.WriteLine("Discovering projects...");
        Console.WriteLine($"Found {projectCount} project(s).");
        Console.WriteLine();
    }

    /// <inheritdoc />
    public void OnProjectStarted(string projectPath, int index, int total) =>
        Console.WriteLine($"[{index}/{total}] {Path.GetFileNameWithoutExtension(projectPath)}");

    /// <inheritdoc />
    public void OnProjectCompleted(string projectPath, int documentCount) =>
        Console.WriteLine($"       {documentCount} entities");

    /// <inheritdoc />
    public void OnProjectFailed(string projectPath, string message) =>
        Console.Error.WriteLine($"       failed: {message}");
}
