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
    private readonly string rootDirectory = Path.Combine(Path.GetTempPath(), "ciir-orchestrator-tests-root-" + Guid.NewGuid());
    private readonly string projectPath;
    private readonly string outputPath = Path.Combine(Path.GetTempPath(), "ciir-orchestrator-tests-" + Guid.NewGuid());

    public AnalyzeInputHandlerTests()
    {
        // DiscoverAuxiliaryFiles walks this directory for every test, so it must be an isolated,
        // otherwise-empty folder rather than the shared OS temp directory itself.
        Directory.CreateDirectory(rootDirectory);
        projectPath = Path.Combine(rootDirectory, "FakeProject.csproj");

        writerFactory.Create(Arg.Any<string>()).Returns(writer);
        reporter.BuildReport().Returns(SuccessfulReport());

        // A real ICiirWriter/IAnalysisArtifactWriter would have created these files by the time
        // the manifest is built (it hashes them); this substitute-based fixture stands them in.
        Directory.CreateDirectory(outputPath);
        File.WriteAllText(Path.Combine(outputPath, "ciir.jsonl"), string.Empty);
        File.WriteAllText(Path.Combine(outputPath, "ciir.schema.json"), string.Empty);

        codeAnalyzer.CanAnalyze(Arg.Any<string>()).Returns(true);

        handler = new AnalyzeInputHandler(
            inputResolver,
            new ProjectDiscoveryService(solutionProjectLister),
            [codeAnalyzer],
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
        codeAnalyzer.DidNotReceiveWithAnyArgs().AnalyzeAsync(default!, default!, default!, TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task ExecuteAsync_StreamsEachDocumentToWriter_AndRecordsIt()
    {
        inputResolver.Resolve(Arg.Any<string>()).Returns(Result<AnalysisInput>.FromSuccess(new AnalysisInput { Type = AnalysisInputType.Project, Path = projectPath }));
        var documents = new[] { TypeDocument("Order"), TypeDocument("Payment") };
        codeAnalyzer.AnalyzeAsync(projectPath, Arg.Any<string>(), Arg.Any<AnalysisOptions>(), Arg.Any<CancellationToken>()).Returns(ToAsyncEnumerable(documents));

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
        codeAnalyzer.AnalyzeAsync(projectPath, Arg.Any<string>(), Arg.Any<AnalysisOptions>(), Arg.Any<CancellationToken>())
            .Returns(_ => throw new InvalidOperationException("boom"));

        var result = await handler.ExecuteAsync(Command(failOnError: false), TestContext.Current.CancellationToken);

        result.ExitCode.ShouldBe(AnalysisExitCode.Success);
        reporter.Received(1).RecordProjectFailed(projectPath, Arg.Any<string>(), "project_load");
    }

    [Fact]
    public async Task ExecuteAsync_ReturnsFailure_WhenProjectFailsWithFailOnError()
    {
        inputResolver.Resolve(Arg.Any<string>()).Returns(Result<AnalysisInput>.FromSuccess(new AnalysisInput { Type = AnalysisInputType.Project, Path = projectPath }));
        codeAnalyzer.AnalyzeAsync(projectPath, Arg.Any<string>(), Arg.Any<AnalysisOptions>(), Arg.Any<CancellationToken>())
            .Returns(_ => throw new InvalidOperationException("boom"));

        var result = await handler.ExecuteAsync(Command(failOnError: true), TestContext.Current.CancellationToken);

        result.ExitCode.ShouldBe(AnalysisExitCode.Failure);
    }

    [Fact]
    public async Task ExecuteAsync_WritesReportManifestAndSchema_AfterAnalysis()
    {
        inputResolver.Resolve(Arg.Any<string>()).Returns(Result<AnalysisInput>.FromSuccess(new AnalysisInput { Type = AnalysisInputType.Project, Path = projectPath }));
        codeAnalyzer.AnalyzeAsync(projectPath, Arg.Any<string>(), Arg.Any<AnalysisOptions>(), Arg.Any<CancellationToken>()).Returns(ToAsyncEnumerable([]));

        await handler.ExecuteAsync(Command(), TestContext.Current.CancellationToken);

        await artifactWriter.Received(1).WriteReportAsync(Arg.Any<AnalysisReport>(), outputPath, Arg.Any<CancellationToken>());
        await artifactWriter.Received(1).WriteSchemaAsync(outputPath, Arg.Any<CancellationToken>());
        await artifactWriter.Received(1).WriteManifestAsync(Arg.Any<AnalysisManifest>(), outputPath, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ExecuteAsync_PassesProjectDirectoryAsRoot_WhenInputIsProject()
    {
        inputResolver.Resolve(Arg.Any<string>()).Returns(Result<AnalysisInput>.FromSuccess(new AnalysisInput { Type = AnalysisInputType.Project, Path = projectPath }));
        codeAnalyzer.AnalyzeAsync(projectPath, Arg.Any<string>(), Arg.Any<AnalysisOptions>(), Arg.Any<CancellationToken>()).Returns(ToAsyncEnumerable([]));

        await handler.ExecuteAsync(Command(), TestContext.Current.CancellationToken);

        var expectedRoot = Path.GetDirectoryName(projectPath)!;
        codeAnalyzer.Received(1).AnalyzeAsync(projectPath, expectedRoot, Arg.Any<AnalysisOptions>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ExecuteAsync_PassesSolutionDirectoryAsRoot_WhenInputIsSolution()
    {
        var solutionPath = Path.Combine(rootDirectory, "Fake.sln");
        inputResolver.Resolve(Arg.Any<string>()).Returns(Result<AnalysisInput>.FromSuccess(new AnalysisInput { Type = AnalysisInputType.Solution, Path = solutionPath }));
        solutionProjectLister.ListProjectPathsAsync(solutionPath, Arg.Any<CancellationToken>()).Returns([projectPath]);
        codeAnalyzer.AnalyzeAsync(projectPath, Arg.Any<string>(), Arg.Any<AnalysisOptions>(), Arg.Any<CancellationToken>()).Returns(ToAsyncEnumerable([]));

        await handler.ExecuteAsync(Command(), TestContext.Current.CancellationToken);

        var expectedRoot = Path.GetDirectoryName(solutionPath)!;
        codeAnalyzer.Received(1).AnalyzeAsync(projectPath, expectedRoot, Arg.Any<AnalysisOptions>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ExecuteAsync_PassesDirectoryItselfAsRoot_WhenInputIsDirectory()
    {
        var directoryInput = Path.Combine(Path.GetTempPath(), "ciir-root-tests-" + Guid.NewGuid());
        var discoveredProjectPath = Path.Combine(directoryInput, "Fake.csproj");
        Directory.CreateDirectory(directoryInput);
        File.WriteAllText(discoveredProjectPath, "<Project />");

        try
        {
            inputResolver.Resolve(Arg.Any<string>()).Returns(Result<AnalysisInput>.FromSuccess(new AnalysisInput { Type = AnalysisInputType.Directory, Path = directoryInput }));
            codeAnalyzer.AnalyzeAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<AnalysisOptions>(), Arg.Any<CancellationToken>()).Returns(ToAsyncEnumerable([]));

            await handler.ExecuteAsync(Command(), TestContext.Current.CancellationToken);

            codeAnalyzer.Received(1).AnalyzeAsync(Arg.Any<string>(), directoryInput, Arg.Any<AnalysisOptions>(), Arg.Any<CancellationToken>());
        }
        finally
        {
            Directory.Delete(directoryInput, recursive: true);
        }
    }

    [Fact]
    public async Task ExecuteAsync_DispatchesAuxiliaryFiles_ToTheAnalyzerThatClaimsThem()
    {
        var yamlPath = Path.Combine(rootDirectory, "docker-compose.yml");
        File.WriteAllText(yamlPath, "version: '3.8'");

        // Overrides the constructor's blanket true so codeAnalyzer no longer also claims the yaml file.
        codeAnalyzer.CanAnalyze(Arg.Any<string>()).Returns(call => string.Equals(Path.GetExtension(call.Arg<string>()), ".csproj", StringComparison.OrdinalIgnoreCase));

        var yamlAnalyzer = Substitute.For<ICodeAnalyzer>();
        yamlAnalyzer.CanAnalyze(Arg.Any<string>()).Returns(call => string.Equals(Path.GetExtension(call.Arg<string>()), ".yml", StringComparison.OrdinalIgnoreCase));
        var yamlDocument = FileDocument("docker-compose.yml");
        yamlAnalyzer.AnalyzeAsync(yamlPath, rootDirectory, Arg.Any<AnalysisOptions>(), Arg.Any<CancellationToken>())
            .Returns(ToAsyncEnumerable([yamlDocument]));

        var multiAnalyzerHandler = new AnalyzeInputHandler(
            inputResolver,
            new ProjectDiscoveryService(solutionProjectLister),
            [codeAnalyzer, yamlAnalyzer],
            writerFactory,
            artifactWriter,
            reporter,
            progress);

        inputResolver.Resolve(Arg.Any<string>()).Returns(Result<AnalysisInput>.FromSuccess(new AnalysisInput { Type = AnalysisInputType.Project, Path = projectPath }));
        codeAnalyzer.AnalyzeAsync(projectPath, rootDirectory, Arg.Any<AnalysisOptions>(), Arg.Any<CancellationToken>()).Returns(ToAsyncEnumerable([]));

        AnalysisManifest? capturedManifest = null;
        await artifactWriter.WriteManifestAsync(Arg.Do<AnalysisManifest>(manifest => capturedManifest = manifest), outputPath, Arg.Any<CancellationToken>());

        var result = await multiAnalyzerHandler.ExecuteAsync(Command(), TestContext.Current.CancellationToken);

        result.ExitCode.ShouldBe(AnalysisExitCode.Success);
        await writer.Received(1).WriteAsync(yamlDocument, Arg.Any<CancellationToken>());
        reporter.Received(1).RecordDocument(yamlDocument);
        capturedManifest.ShouldNotBeNull();
        capturedManifest!.Projects.ShouldHaveSingleItem();
        capturedManifest.Projects[0].Path.ShouldBe(projectPath);
    }

    public void Dispose()
    {
        if (Directory.Exists(outputPath))
        {
            Directory.Delete(outputPath, recursive: true);
        }

        if (Directory.Exists(rootDirectory))
        {
            Directory.Delete(rootDirectory, recursive: true);
        }
    }

    private static AnalysisReport SuccessfulReport() => new()
    {
        Success = true,
        Projects = new AnalysisProjectStats { Discovered = 1, Analyzed = 1, Failed = 0 },
        Documents = new AnalysisDocumentStats { Analyzed = 0, Ignored = 0, FilesAnalyzed = 0 },
        Relations = new AnalysisRelationStats { Resolved = 0, Unresolved = 0 },
        Entities = new AnalysisEntityCounts { Types = 0, Methods = 0, ConfigurationKeys = 0, Files = 0 },
    };

    private static CiirDocument TypeDocument(string name) => new()
    {
        Id = $"sha256:{name}",
        Kind = CiirKind.Type,
        Language = "csharp",
        Project = "FakeProject",
        Symbol = new CiirSymbol { Name = name, QualifiedName = name, CanonicalName = name },
    };

    private static CiirDocument FileDocument(string name) => new()
    {
        Id = $"sha256:{name}",
        Kind = CiirKind.File,
        Language = "yaml",
        Project = "Configuration",
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
