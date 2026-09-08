using Ciir.Application.Model;

namespace Ciir.Application.Ports;

/// <summary>Writes the non-CIIR output artifacts: <c>manifest.json</c>, <c>analysis-report.json</c>, and <c>ciir.schema.json</c>.</summary>
public interface IAnalysisArtifactWriter
{
    /// <summary>Writes <c>analysis-report.json</c> to <paramref name="outputDirectory"/>.</summary>
    /// <param name="report">The report to write.</param>
    /// <param name="outputDirectory">The output directory.</param>
    /// <param name="cancellationToken">A token to observe for cancellation.</param>
    Task WriteReportAsync(AnalysisReport report, string outputDirectory, CancellationToken cancellationToken);

    /// <summary>Writes <c>manifest.json</c> to <paramref name="outputDirectory"/>.</summary>
    /// <param name="manifest">The manifest to write.</param>
    /// <param name="outputDirectory">The output directory.</param>
    /// <param name="cancellationToken">A token to observe for cancellation.</param>
    Task WriteManifestAsync(AnalysisManifest manifest, string outputDirectory, CancellationToken cancellationToken);

    /// <summary>Copies the embedded <c>ciir.schema.json</c> contract to <paramref name="outputDirectory"/>.</summary>
    /// <param name="outputDirectory">The output directory.</param>
    /// <param name="cancellationToken">A token to observe for cancellation.</param>
    Task WriteSchemaAsync(string outputDirectory, CancellationToken cancellationToken);
}
