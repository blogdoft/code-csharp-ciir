using Ciir.Core;
using Shouldly;

namespace Ciir.Configuration.Tests;

public class YamlFileAnalyzerTests : IDisposable
{
    private readonly string rootDirectory = Path.Combine(Path.GetTempPath(), "ciir-yaml-tests-" + Guid.NewGuid());

    public YamlFileAnalyzerTests()
    {
        Directory.CreateDirectory(rootDirectory);
    }

    [Fact]
    public async Task AnalyzeAsync_CapturesPathHashAndSize_WithoutParsingContent()
    {
        var content = "version: '3.8'\nservices:\n  web:\n    image: nginx\n";
        var path = WriteFile("docker-compose.yml", content);

        var document = await YamlFileAnalyzer.AnalyzeAsync(path, rootDirectory, TestContext.Current.CancellationToken);

        document.Kind.ShouldBe(CiirKind.File);
        document.Language.ShouldBe("yaml");
        document.Symbol.QualifiedName.ShouldBe("docker-compose.yml");
        document.Source.ShouldNotBeNull();
        document.Source!.Hash.ShouldNotBeNullOrEmpty();
        document.File.ShouldNotBeNull();
        document.File!.SizeBytes.ShouldBe((long)System.Text.Encoding.UTF8.GetByteCount(content));
        document.Source.Text.ShouldBeNull();
    }

    [Fact]
    public async Task AnalyzeAsync_ProducesValidFileDocument_EvenForGarbageYamlText()
    {
        var path = WriteFile("broken.yaml", "::: not valid: yaml: [[[ at all");

        var document = await YamlFileAnalyzer.AnalyzeAsync(path, rootDirectory, TestContext.Current.CancellationToken);

        document.Kind.ShouldBe(CiirKind.File);
        document.File.ShouldNotBeNull();
    }

    [Fact]
    public async Task AnalyzeAsync_IsDeterministic_AcrossRuns()
    {
        var path = WriteFile("values.yml", "key: value\n");

        var first = await YamlFileAnalyzer.AnalyzeAsync(path, rootDirectory, TestContext.Current.CancellationToken);
        var second = await YamlFileAnalyzer.AnalyzeAsync(path, rootDirectory, TestContext.Current.CancellationToken);

        first.Id.ShouldBe(second.Id);
    }

    public void Dispose()
    {
        if (Directory.Exists(rootDirectory))
        {
            Directory.Delete(rootDirectory, recursive: true);
        }
    }

    private string WriteFile(string fileName, string content)
    {
        var fullPath = Path.Combine(rootDirectory, fileName);
        File.WriteAllText(fullPath, content);
        return fullPath;
    }
}
