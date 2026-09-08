using Ciir.Application.Model;
using Ciir.Application.Ports;
using Ciir.Core;
using Ciir.Core.Relations;

namespace Ciir.Application.Reporting;

/// <summary>Thread-unsafe, in-memory accumulator backing <see cref="IAnalysisReporter"/>.</summary>
public sealed class AnalysisReporter : IAnalysisReporter
{
    private readonly HashSet<string> discoveredProjects = new(StringComparer.OrdinalIgnoreCase);
    private readonly HashSet<string> analyzedProjects = new(StringComparer.OrdinalIgnoreCase);
    private readonly HashSet<string> filePaths = new(StringComparer.Ordinal);
    private readonly List<AnalysisError> errors = [];

    private int documentsAnalyzed;
    private int typesAnalyzed;
    private int methodsAnalyzed;
    private int relationsResolved;
    private int relationsUnresolved;

    /// <inheritdoc />
    public void RecordProjectDiscovered(string projectPath) => discoveredProjects.Add(projectPath);

    /// <inheritdoc />
    public void RecordProjectAnalyzed(string projectPath) => analyzedProjects.Add(projectPath);

    /// <inheritdoc />
    public void RecordProjectFailed(string projectPath, string message, string category) =>
        errors.Add(new AnalysisError { Project = projectPath, Message = message, Category = category });

    /// <inheritdoc />
    public void RecordDocument(CiirDocument document)
    {
        ArgumentNullException.ThrowIfNull(document);

        documentsAnalyzed++;

        if (document.Source is { } source)
        {
            filePaths.Add(source.Path);
        }

        if (document.Kind == CiirKind.Type)
        {
            typesAnalyzed++;
        }
        else if (document.Kind is CiirKind.Method or CiirKind.Constructor)
        {
            methodsAnalyzed++;
        }

        foreach (var relation in document.Relations)
        {
            if (relation.Resolution.Status is CiirResolutionStatus.Resolved or CiirResolutionStatus.External)
            {
                relationsResolved++;
            }
            else
            {
                relationsUnresolved++;
            }
        }
    }

    /// <inheritdoc />
    public AnalysisReport BuildReport() => new()
    {
        Success = errors.Count == 0,
        Projects = new AnalysisProjectStats
        {
            Discovered = discoveredProjects.Count,
            Analyzed = analyzedProjects.Count,
            Failed = errors.Count,
        },
        Documents = new AnalysisDocumentStats
        {
            Analyzed = documentsAnalyzed,
            Ignored = 0,
            FilesAnalyzed = filePaths.Count,
        },
        Relations = new AnalysisRelationStats
        {
            Resolved = relationsResolved,
            Unresolved = relationsUnresolved,
        },
        Entities = new AnalysisEntityCounts
        {
            Types = typesAnalyzed,
            Methods = methodsAnalyzed,
        },
        Errors = [.. errors],
    };
}
