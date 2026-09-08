using Ciir.Application.Model;
using Ciir.Application.Ports;
using Ciir.Serialization.Json;
using Ciir.Serialization.SchemaProvider;
using System.Text.Json;

namespace Ciir.Serialization.Writing;

/// <summary>Writes <c>manifest.json</c>, <c>analysis-report.json</c>, and copies <c>ciir.schema.json</c> to the output directory.</summary>
public sealed class AnalysisArtifactWriter : IAnalysisArtifactWriter
{
    private readonly JsonSerializerOptions options = CiirJsonSerializerOptions.Create();

    /// <inheritdoc />
    public Task WriteReportAsync(AnalysisReport report, string outputDirectory, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(report);

        var payload = new
        {
            success = report.Success,
            projects = new
            {
                discovered = report.Projects.Discovered,
                analyzed = report.Projects.Analyzed,
                failed = report.Projects.Failed,
            },
            documents = new
            {
                analyzed = report.Documents.Analyzed,
                ignored = report.Documents.Ignored,
            },
            relations = new
            {
                resolved = report.Relations.Resolved,
                unresolved = report.Relations.Unresolved,
            },
            errors = report.Errors.Select(error => new
            {
                project = error.Project,
                message = error.Message,
                category = error.Category,
            }),
        };

        return WriteJsonFileAsync(Path.Combine(outputDirectory, "analysis-report.json"), payload, cancellationToken);
    }

    /// <inheritdoc />
    public Task WriteManifestAsync(AnalysisManifest manifest, string outputDirectory, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(manifest);

        var payload = new
        {
            format = "ciir",
            schemaVersion = Core.SchemaVersion.Current,
            generator = new { name = "ciir-csharp", version = manifest.GeneratorVersion },
            input = new { type = manifest.InputType, path = manifest.InputPath },
            generatedAt = manifest.GeneratedAt.UtcDateTime.ToString("yyyy-MM-ddTHH:mm:ssZ", System.Globalization.CultureInfo.InvariantCulture),
            projects = manifest.Projects.Select(project => new { name = project.Name, path = project.Path }),
            files = manifest.Files.Select(file => new { path = file.Path, records = file.Records, sha256 = file.Sha256 }),
            statistics = new
            {
                projects = manifest.Statistics.Projects,
                filesAnalyzed = manifest.Statistics.FilesAnalyzed,
                types = manifest.Statistics.Types,
                methods = manifest.Statistics.Methods,
                relations = manifest.Statistics.Relations,
                unresolvedRelations = manifest.Statistics.UnresolvedRelations,
            },
        };

        return WriteJsonFileAsync(Path.Combine(outputDirectory, "manifest.json"), payload, cancellationToken);
    }

    /// <inheritdoc />
    public Task WriteSchemaAsync(string outputDirectory, CancellationToken cancellationToken) =>
        File.WriteAllTextAsync(Path.Combine(outputDirectory, "ciir.schema.json"), CiirSchemaProvider.GetSchemaJson(), cancellationToken);

    private async Task WriteJsonFileAsync(string path, object payload, CancellationToken cancellationToken)
    {
        await using var stream = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.None);
        await JsonSerializer.SerializeAsync(stream, payload, options, cancellationToken);
    }
}
