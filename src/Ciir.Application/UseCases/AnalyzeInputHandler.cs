using Ciir.Application.Model;
using Ciir.Application.Ports;
using Ciir.Application.ProjectDiscovery;
using Ciir.Core.Hashing;

namespace Ciir.Application.UseCases;

/// <summary>
/// The main analysis use case: resolve input, discover projects, analyze each one while
/// streaming CIIR output, then write the manifest and report. Invokable programmatically with no
/// dependency on any specific delivery mechanism (CLI, HTTP, ...).
/// </summary>
public sealed class AnalyzeInputHandler(
    IInputResolver inputResolver,
    ProjectDiscoveryService projectDiscovery,
    ICodeAnalyzer codeAnalyzer,
    ICiirWriterFactory writerFactory,
    IAnalysisArtifactWriter artifactWriter,
    IAnalysisReporter reporter,
    IAnalysisProgressReporter progress)
{
    /// <summary>Runs the analysis described by <paramref name="command"/>.</summary>
    /// <param name="command">The analysis request.</param>
    /// <param name="cancellationToken">A token to observe for cancellation.</param>
    public async Task<AnalysisResult> ExecuteAsync(AnalyzeInputCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        var inputResult = inputResolver.Resolve(command.Path);
        if (inputResult.IsFailure)
        {
            return new AnalysisResult { ExitCode = AnalysisExitCode.InvalidInput, ErrorMessage = inputResult.Failure.Message };
        }

        var rootDirectory = inputResult.Value.Type == AnalysisInputType.Directory
            ? inputResult.Value.Path
            : Path.GetDirectoryName(inputResult.Value.Path)!;

        IReadOnlyList<string> projectPaths;
        try
        {
            projectPaths = await projectDiscovery.DiscoverAsync(inputResult.Value, cancellationToken);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            return new AnalysisResult { ExitCode = AnalysisExitCode.InvalidInput, ErrorMessage = $"Failed to discover projects: {ex.Message}" };
        }

        progress.OnProjectsDiscovered(projectPaths.Count);
        foreach (var projectPath in projectPaths)
        {
            reporter.RecordProjectDiscovered(projectPath);
        }

        try
        {
            Directory.CreateDirectory(command.Options.OutputPath);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            return new AnalysisResult { ExitCode = AnalysisExitCode.OutputWriteFailure, ErrorMessage = ex.Message };
        }

        var hasFatalProjectFailure = await AnalyzeProjectsAsync(projectPaths, rootDirectory, command.Options, cancellationToken);
        var report = reporter.BuildReport();

        try
        {
            await WriteArtifactsAsync(command, inputResult.Value.Type, projectPaths, report, cancellationToken);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            return new AnalysisResult { ExitCode = AnalysisExitCode.OutputWriteFailure, ErrorMessage = ex.Message, Report = report };
        }

        var exitCode = hasFatalProjectFailure && command.Options.FailOnError ? AnalysisExitCode.Failure : AnalysisExitCode.Success;
        return new AnalysisResult { ExitCode = exitCode, Report = report };
    }

    private static AnalysisManifest BuildManifest(
        AnalyzeInputCommand command,
        AnalysisInputType inputType,
        IReadOnlyList<string> projectPaths,
        AnalysisReport report)
    {
        var jsonlPath = Path.Combine(command.Options.OutputPath, "ciir.jsonl");
        var schemaPath = Path.Combine(command.Options.OutputPath, "ciir.schema.json");

        return new AnalysisManifest
        {
            GeneratorVersion = typeof(AnalyzeInputHandler).Assembly.GetName().Version?.ToString() ?? "0.0.0",
            InputType = inputType,
            InputPath = command.Path,
            GeneratedAt = DateTimeOffset.UtcNow,
            Projects = [.. projectPaths.Select(path => new AnalysisManifestProject
            {
                Name = Path.GetFileNameWithoutExtension(path),
                Path = path,
            })],
            Files =
            [
                new AnalysisManifestFile { Path = "ciir.jsonl", Records = report.Documents.Analyzed, Sha256 = HashFile(jsonlPath) },
                new AnalysisManifestFile { Path = "ciir.schema.json", Sha256 = HashFile(schemaPath) },
            ],
            Statistics = new AnalysisManifestStatistics
            {
                Projects = report.Projects.Analyzed,
                FilesAnalyzed = report.Documents.FilesAnalyzed,
                Types = report.Entities.Types,
                Methods = report.Entities.Methods,
                Relations = report.Relations.Resolved + report.Relations.Unresolved,
                UnresolvedRelations = report.Relations.Unresolved,
            },
        };
    }

    private static string HashFile(string path) => Sha256Text.ComputePrefixedHash(File.ReadAllBytes(path));

    private async Task<bool> AnalyzeProjectsAsync(IReadOnlyList<string> projectPaths, string rootDirectory, AnalysisOptions options, CancellationToken cancellationToken)
    {
        var hasFatalProjectFailure = false;

        await using var writer = writerFactory.Create(options.OutputPath);

        for (var index = 0; index < projectPaths.Count; index++)
        {
            var projectPath = projectPaths[index];
            progress.OnProjectStarted(projectPath, index + 1, projectPaths.Count);

            try
            {
                var documentCount = 0;
                await foreach (var document in codeAnalyzer.AnalyzeAsync(projectPath, rootDirectory, options, cancellationToken))
                {
                    reporter.RecordDocument(document);
                    await writer.WriteAsync(document, cancellationToken);
                    documentCount++;
                }

                reporter.RecordProjectAnalyzed(projectPath);
                progress.OnProjectCompleted(projectPath, documentCount);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                reporter.RecordProjectFailed(projectPath, ex.Message, "project_load");
                progress.OnProjectFailed(projectPath, ex.Message);
                hasFatalProjectFailure = true;
            }
        }

        return hasFatalProjectFailure;
    }

    private async Task WriteArtifactsAsync(
        AnalyzeInputCommand command,
        AnalysisInputType inputType,
        IReadOnlyList<string> projectPaths,
        AnalysisReport report,
        CancellationToken cancellationToken)
    {
        await artifactWriter.WriteReportAsync(report, command.Options.OutputPath, cancellationToken);
        await artifactWriter.WriteSchemaAsync(command.Options.OutputPath, cancellationToken);

        var manifest = BuildManifest(command, inputType, projectPaths, report);
        await artifactWriter.WriteManifestAsync(manifest, command.Options.OutputPath, cancellationToken);
    }
}
