using Ciir.Application.Model;
using Ciir.Application.Ports;
using Ciir.Application.ProjectDiscovery;
using NSubstitute;
using Shouldly;

namespace Ciir.Application.Tests.ProjectDiscovery;

public class ProjectDiscoveryServiceTests : IDisposable
{
    private readonly string rootDirectory = Directory.CreateTempSubdirectory("ciir-discovery-tests-").FullName;
    private readonly ISolutionProjectLister solutionProjectLister = Substitute.For<ISolutionProjectLister>();

    [Fact]
    public async Task DiscoverAsync_ReturnsSingleProject_ForProjectInput()
    {
        var projectPath = CreateFile("App.csproj");
        var service = new ProjectDiscoveryService(solutionProjectLister);

        var result = await service.DiscoverAsync(
            new AnalysisInput { Type = AnalysisInputType.Project, Path = projectPath },
            TestContext.Current.CancellationToken);

        result.ShouldBe([Path.GetFullPath(projectPath)]);
    }

    [Fact]
    public async Task DiscoverAsync_DelegatesToSolutionProjectLister_ForSolutionInput()
    {
        var solutionPath = CreateFile("App.sln");
        var projectA = CreateFile("A.csproj");
        var projectB = CreateFile("B.csproj");
        solutionProjectLister.ListProjectPathsAsync(solutionPath, Arg.Any<CancellationToken>())
            .Returns([projectA, projectB]);
        var service = new ProjectDiscoveryService(solutionProjectLister);

        var result = await service.DiscoverAsync(
            new AnalysisInput { Type = AnalysisInputType.Solution, Path = solutionPath },
            TestContext.Current.CancellationToken);

        result.ShouldBe([Path.GetFullPath(projectA), Path.GetFullPath(projectB)], ignoreOrder: true);
    }

    [Fact]
    public async Task DiscoverAsync_DeduplicatesProjectAlreadyReferencedBySolution()
    {
        // Root/
        //   App.sln (references Domain.csproj, Application.csproj)
        //   services/Billing/Billing.sln (references Billing.Domain.csproj)
        //   src/Domain/Domain.csproj
        //   src/Application/Application.csproj
        //   services/Billing/Billing.Domain.csproj
        //   tools/Tool/Tool.csproj (not referenced by any solution)
        var domain = CreateFile("src/Domain/Domain.csproj");
        var application = CreateFile("src/Application/Application.csproj");
        var billingDomain = CreateFile("services/Billing/Billing.Domain.csproj");
        var tool = CreateFile("tools/Tool/Tool.csproj");
        var appSolution = CreateFile("App.sln");
        var billingSolution = CreateFile("services/Billing/Billing.sln");

        solutionProjectLister.ListProjectPathsAsync(appSolution, Arg.Any<CancellationToken>())
            .Returns([domain, application]);
        solutionProjectLister.ListProjectPathsAsync(billingSolution, Arg.Any<CancellationToken>())
            .Returns([billingDomain]);

        var service = new ProjectDiscoveryService(solutionProjectLister);

        var result = await service.DiscoverAsync(
            new AnalysisInput { Type = AnalysisInputType.Directory, Path = rootDirectory },
            TestContext.Current.CancellationToken);

        result.ShouldBe(
            [Path.GetFullPath(domain), Path.GetFullPath(application), Path.GetFullPath(billingDomain), Path.GetFullPath(tool)],
            ignoreOrder: true);
        result.Count.ShouldBe(4);
    }

    [Fact]
    public async Task DiscoverAsync_IgnoresBinObjGitAndVsDirectories()
    {
        CreateFile("bin/Debug/Generated.csproj");
        CreateFile("obj/Generated.csproj");
        CreateFile(".git/Fake.csproj");
        CreateFile(".vs/Fake.csproj");
        var realProject = CreateFile("src/Real.csproj");
        var service = new ProjectDiscoveryService(solutionProjectLister);

        var result = await service.DiscoverAsync(
            new AnalysisInput { Type = AnalysisInputType.Directory, Path = rootDirectory },
            TestContext.Current.CancellationToken);

        result.ShouldBe([Path.GetFullPath(realProject)]);
    }

    [Fact]
    public async Task DiscoverAsync_ReturnsResultsInDeterministicOrder()
    {
        CreateFile("Zebra/Zebra.csproj");
        CreateFile("Alpha/Alpha.csproj");
        var service = new ProjectDiscoveryService(solutionProjectLister);
        var input = new AnalysisInput { Type = AnalysisInputType.Directory, Path = rootDirectory };

        var first = await service.DiscoverAsync(input, TestContext.Current.CancellationToken);
        var second = await service.DiscoverAsync(input, TestContext.Current.CancellationToken);

        first.ShouldBe(second);
        first.ShouldBe([.. first.OrderBy(p => p, StringComparer.OrdinalIgnoreCase)]);
    }

    [Fact]
    public void DiscoverAuxiliaryFiles_FindsLooseFiles_AtRootAndInSubdirectories()
    {
        var appsettings = CreateFile("appsettings.json");
        var yaml = CreateFile("infra/docker-compose.yml");

        var result = ProjectDiscoveryService.DiscoverAuxiliaryFiles(rootDirectory);

        result.ShouldBe([Path.GetFullPath(appsettings), Path.GetFullPath(yaml)], ignoreOrder: true);
    }

    [Fact]
    public void DiscoverAuxiliaryFiles_ExcludesIgnoredDirectories()
    {
        CreateFile("bin/Debug/appsettings.json");
        CreateFile("obj/appsettings.json");
        CreateFile(".git/config.yaml");
        CreateFile(".vs/config.yaml");
        CreateFile("node_modules/pkg/config.yaml");
        var real = CreateFile("src/appsettings.json");

        var result = ProjectDiscoveryService.DiscoverAuxiliaryFiles(rootDirectory);

        result.ShouldBe([Path.GetFullPath(real)]);
    }

    [Fact]
    public void DiscoverAuxiliaryFiles_ExcludesProjectSolutionAndCSharpFiles()
    {
        CreateFile("App.csproj");
        CreateFile("App.sln");
        CreateFile("Program.cs");
        var yaml = CreateFile("values.yaml");

        var result = ProjectDiscoveryService.DiscoverAuxiliaryFiles(rootDirectory);

        result.ShouldBe([Path.GetFullPath(yaml)]);
    }

    public void Dispose() => Directory.Delete(rootDirectory, recursive: true);

    private string CreateFile(string relativePath)
    {
        var fullPath = Path.Combine(rootDirectory, relativePath);
        Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);
        File.WriteAllText(fullPath, string.Empty);
        return fullPath;
    }
}
