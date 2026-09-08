using Ciir.Application.InputResolution;
using Ciir.Application.Model;
using Shouldly;

namespace Ciir.Application.Tests.InputResolution;

public class InputResolverTests : IDisposable
{
    private readonly string tempDirectory = Directory.CreateTempSubdirectory("ciir-input-resolver-tests-").FullName;
    private readonly InputResolver resolver = new();

    [Fact]
    public void Resolve_ReturnsSolutionType_ForSlnFile()
    {
        var path = CreateFile("App.sln");

        var result = resolver.Resolve(path);

        result.IsSuccess.ShouldBeTrue();
        result.Value.Type.ShouldBe(AnalysisInputType.Solution);
    }

    [Fact]
    public void Resolve_ReturnsSolutionType_ForSlnxFile()
    {
        var path = CreateFile("App.slnx");

        var result = resolver.Resolve(path);

        result.IsSuccess.ShouldBeTrue();
        result.Value.Type.ShouldBe(AnalysisInputType.Solution);
    }

    [Fact]
    public void Resolve_ReturnsProjectType_ForCsprojFile()
    {
        var path = CreateFile("App.csproj");

        var result = resolver.Resolve(path);

        result.IsSuccess.ShouldBeTrue();
        result.Value.Type.ShouldBe(AnalysisInputType.Project);
    }

    [Fact]
    public void Resolve_ReturnsDirectoryType_ForDirectory()
    {
        var result = resolver.Resolve(tempDirectory);

        result.IsSuccess.ShouldBeTrue();
        result.Value.Type.ShouldBe(AnalysisInputType.Directory);
    }

    [Fact]
    public void Resolve_ReturnsFullyQualifiedPath()
    {
        var path = CreateFile("App.csproj");

        var result = resolver.Resolve(path);

        result.Value.Path.ShouldBe(Path.GetFullPath(path));
    }

    [Fact]
    public void Resolve_Fails_WhenPathDoesNotExist()
    {
        var result = resolver.Resolve(Path.Combine(tempDirectory, "does-not-exist.sln"));

        result.IsFailure.ShouldBeTrue();
        result.Failure.Code.ShouldBe("path_not_found");
    }

    [Fact]
    public void Resolve_Fails_ForUnsupportedFileExtension()
    {
        var path = CreateFile("readme.md");

        var result = resolver.Resolve(path);

        result.IsFailure.ShouldBeTrue();
        result.Failure.Code.ShouldBe("unsupported_file_type");
    }

    public void Dispose() => Directory.Delete(tempDirectory, recursive: true);

    private string CreateFile(string fileName)
    {
        var path = Path.Combine(tempDirectory, fileName);
        File.WriteAllText(path, string.Empty);
        return path;
    }
}
