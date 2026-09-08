using BlogDoFT.Libs.ResultPattern;
using Ciir.Application.Model;
using Ciir.Application.Ports;
using Ciir.Application.ProjectDiscovery;
using Ciir.Application.UseCases;
using Ciir.Core;
using Ciir.Core.Symbols;
using NSubstitute;
using Shouldly;

namespace Ciir.Application.Tests.UseCases;

public class AnalyzeInputHandlerTests : IDisposable
{
    private readonly IInputResolver inputResolver = Substitute.For<IInputResolver>();
    private readonly ISolutionProjectLister solutionProjectLister = Substitute.For<ISolutionProjectLister>();
    private readonly ICodeAnalyzer codeAnalyzer = Substitute.For<ICodeAnalyzer>();
    private readonly ICiirWriterFactory writerFactory = Substitute.For<ICiirWriterFactory>();
    private readonly ICiirWriter writer = Substitute.For<ICiirWriter>();
    private readonly IAnalysisArtifactWriter artifactWriter = Substitute.For<IAnalysisArtifactWriter>();
    private readonly IAnalysisReporter reporter = Substitute.For<IAnalysisReporter>();
    private readonly IAnalysisProgressReporter progress = Substitute.For<IAnalysisProgressReporter>();

    private readonly AnalyzeInputHandler handler;
    private readonly string projectPath = Path.Combine(Path.GetTempPath(), "FakeProject.csproj");
    private readonly string outputPath = Path.Combine(Path.GetTempPath(), "ciir-orchestrator-tests-" + Guid.NewGuid());

    public AnalyzeInputHandlerTests()
    {
        writerFactory.Create(Arg.Any<string>()).Returns(writer);
        reporter.BuildReport().Returns(SuccessfulReport());

        // A real ICiirWriter/IAnalysisArtifactWriter would have created these files by the time
        // the manifest is built (it hashes them); this substitute-based fixture stands them in.
        Directory.CreateDirectory(outputPath);
        File.WriteAllText(Path.Combine(outputPath, "ciir.jsonl"), string.Empty);
        File.WriteAllText(Path.Combine(outputPath, "ciir.schema.json"), string.Empty);

        handler = new AnalyzeInputHandler(
            inputResolver,
            new ProjectDiscoveryService(solutionProjectLister),
            codeAnalyzer,
            writerFactory,
            artifactWriter,
            reporter,
            progress);
    }

    [Fact]
    public async Task ExecuteAsync_ReturnsInvalidInput_WhenInputResolutionFails()
    {
        inputResolver.Resolve(Arg.Any<string>()).Returns(Result<AnalysisInput>.FromFailure(new Failure("path_not_found", "nope")));

        var result = await handler.ExecuteAsync(Command(), TestContext.Current.CancellationToken);

        result.ExitCode.ShouldBe(AnalysisExitCode.InvalidInput);
        codeAnalyzer.DidNotReceiveWithAnyArgs().AnalyzeAsync(default!, default!, TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task ExecuteAsync_StreamsEachDocumentToWriter_AndRecordsIt()
    {
        inputResolver.Resolve(Arg.Any<string>()).Returns(Result<AnalysisInput>.FromSuccess(new AnalysisInput { Type = AnalysisInputType.Project, Path = projectPath }));
        var documents = new[] { TypeDocument("Order"), TypeDocument("Payment") };
        codeAnalyzer.AnalyzeAsync(projectPath, Arg.Any<AnalysisOptions>(), Arg.Any<CancellationToken>()).Returns(ToAsyncEnumerable(documents));

        var result = await handler.ExecuteAsync(Command(), TestContext.Current.CancellationToken);

        result.ExitCode.ShouldBe(AnalysisExitCode.Success);
        await writer.Received(1).WriteAsync(documents[0], Arg.Any<CancellationToken>());
        await writer.Received(1).WriteAsync(documents[1], Arg.Any<CancellationToken>());
        reporter.Received(1).RecordDocument(documents[0]);
        reporter.Received(1).RecordDocument(documents[1]);
        reporter.Received(1).RecordProjectAnalyzed(projectPath);
    }

    [Fact]
    public async Task ExecuteAsync_ContinuesAndSucceeds_WhenProjectFailsWithoutFailOnError()
    {
        inputResolver.Resolve(Arg.Any<string>()).Returns(Result<AnalysisInput>.FromSuccess(new AnalysisInput { Type = AnalysisInputType.Project, Path = projectPath }));
        codeAnalyzer.AnalyzeAsync(projectPath, Arg.Any<AnalysisOptions>(), Arg.Any<CancellationToken>())
            .Returns(_ => throw new InvalidOperationException("boom"));

        var result = await handler.ExecuteAsync(Command(failOnError: false), TestContext.Current.CancellationToken);

        result.ExitCode.ShouldBe(AnalysisExitCode.Success);
        reporter.Received(1).RecordProjectFailed(projectPath, Arg.Any<string>(), "project_load");
    }

    [Fact]
    public async Task ExecuteAsync_ReturnsFailure_WhenProjectFailsWithFailOnError()
    {
        inputResolver.Resolve(Arg.Any<string>()).Returns(Result<AnalysisInput>.FromSuccess(new AnalysisInput { Type = AnalysisInputType.Project, Path = projectPath }));
        codeAnalyzer.AnalyzeAsync(projectPath, Arg.Any<AnalysisOptions>(), Arg.Any<CancellationToken>())
            .Returns(_ => throw new InvalidOperationException("boom"));

        var result = await handler.ExecuteAsync(Command(failOnError: true), TestContext.Current.CancellationToken);

        result.ExitCode.ShouldBe(AnalysisExitCode.Failure);
    }

    [Fact]
    public async Task ExecuteAsync_WritesReportManifestAndSchema_AfterAnalysis()
    {
        inputResolver.Resolve(Arg.Any<string>()).Returns(Result<AnalysisInput>.FromSuccess(new AnalysisInput { Type = AnalysisInputType.Project, Path = projectPath }));
        codeAnalyzer.AnalyzeAsync(projectPath, Arg.Any<AnalysisOptions>(), Arg.Any<CancellationToken>()).Returns(ToAsyncEnumerable([]));

        await handler.ExecuteAsync(Command(), TestContext.Current.CancellationToken);

        await artifactWriter.Received(1).WriteReportAsync(Arg.Any<AnalysisReport>(), outputPath, Arg.Any<CancellationToken>());
        await artifactWriter.Received(1).WriteSchemaAsync(outputPath, Arg.Any<CancellationToken>());
        await artifactWriter.Received(1).WriteManifestAsync(Arg.Any<AnalysisManifest>(), outputPath, Arg.Any<CancellationToken>());
    }

    public void Dispose()
    {
        if (Directory.Exists(outputPath))
        {
            Directory.Delete(outputPath, recursive: true);
        }
    }

    private static AnalysisReport SuccessfulReport() => new()
    {
        Success = true,
        Projects = new AnalysisProjectStats { Discovered = 1, Analyzed = 1, Failed = 0 },
        Documents = new AnalysisDocumentStats { Analyzed = 0, Ignored = 0, FilesAnalyzed = 0 },
        Relations = new AnalysisRelationStats { Resolved = 0, Unresolved = 0 },
        Entities = new AnalysisEntityCounts { Types = 0, Methods = 0 },
    };

    private static CiirDocument TypeDocument(string name) => new()
    {
        Id = $"sha256:{name}",
        Kind = CiirKind.Type,
        Language = "csharp",
        Project = "FakeProject",
        Symbol = new CiirSymbol { Name = name, QualifiedName = name, CanonicalName = name },
    };

    private static async IAsyncEnumerable<CiirDocument> ToAsyncEnumerable(IEnumerable<CiirDocument> documents)
    {
        foreach (var document in documents)
        {
            yield return document;
        }

        await Task.CompletedTask;
    }

    private AnalyzeInputCommand Command(bool failOnError = false) => new()
    {
        Path = projectPath,
        Options = new AnalysisOptions { OutputPath = outputPath, FailOnError = failOnError },
    };
}
