using Ciir.Core;
using Ciir.Core.Configuration;
using Shouldly;

namespace Ciir.Configuration.Tests;

public class AppSettingsAnalyzerTests : IDisposable
{
    private const string SecretSentinel = "Server=SECRET_SENTINEL;Password=hunter2";

    private readonly string rootDirectory = Path.Combine(Path.GetTempPath(), "ciir-appsettings-tests-" + Guid.NewGuid());

    public AppSettingsAnalyzerTests()
    {
        Directory.CreateDirectory(rootDirectory);
    }

    [Fact]
    public async Task AnalyzeAsync_EmitsConfigurationDocument_ForTheFileItself()
    {
        var path = WriteFile("appsettings.json", """{ "Foo": "bar" }""");

        var documents = await CollectAsync(path);

        var configuration = documents.Single(d => d.Kind == CiirKind.Configuration);
        configuration.Symbol.QualifiedName.ShouldBe("appsettings.json");
        configuration.Language.ShouldBe("json");
    }

    [Fact]
    public async Task AnalyzeAsync_FlattensNestedKeys_UsingColonSeparatedPaths()
    {
        const string json = """
            {
              "ConnectionStrings": { "Default": "value" },
              "Feature": { "Nested": { "Deep": 1 } }
            }
            """;
        var path = WriteFile("appsettings.json", json);

        var documents = await CollectAsync(path);
        var keyPaths = documents.Where(d => d.Kind == CiirKind.ConfigurationKey).Select(d => d.Symbol.QualifiedName).ToArray();

        keyPaths.ShouldContain($"{path.RelativeName}:ConnectionStrings");
        keyPaths.ShouldContain($"{path.RelativeName}:ConnectionStrings:Default");
        keyPaths.ShouldContain($"{path.RelativeName}:Feature:Nested:Deep");
    }

    [Fact]
    public async Task AnalyzeAsync_FlattensArrayElements_UsingNumericIndices()
    {
        var path = WriteFile("appsettings.json", """{ "AllowedHosts": ["a", "b"] }""");

        var documents = await CollectAsync(path);
        var keyPaths = documents.Where(d => d.Kind == CiirKind.ConfigurationKey).Select(d => d.Symbol.QualifiedName).ToArray();

        keyPaths.ShouldContain($"{path.RelativeName}:AllowedHosts:0");
        keyPaths.ShouldContain($"{path.RelativeName}:AllowedHosts:1");
    }

    [Theory]
    [InlineData("""{"K": "text"}""", CiirConfigurationValueKind.StringValue)]
    [InlineData("""{"K": 42}""", CiirConfigurationValueKind.NumberValue)]
    [InlineData("""{"K": true}""", CiirConfigurationValueKind.BooleanValue)]
    [InlineData("""{"K": null}""", CiirConfigurationValueKind.NullValue)]
    [InlineData("""{"K": [1]}""", CiirConfigurationValueKind.ArrayValue)]
    [InlineData("""{"K": {"Nested": 1}}""", CiirConfigurationValueKind.ObjectValue)]
    public async Task AnalyzeAsync_MapsJsonValueKind_ToExpectedConfigurationValueKind(string json, CiirConfigurationValueKind expected)
    {
        var path = WriteFile("appsettings.json", json);

        var documents = await CollectAsync(path);
        var key = documents.Single(d => d.Kind == CiirKind.ConfigurationKey && string.Equals(d.Symbol.Name, "K", StringComparison.Ordinal));

        key.ConfigurationKey.ShouldNotBeNull();
        key.ConfigurationKey!.ValueType.ShouldBe(expected);
    }

    [Fact]
    public async Task AnalyzeAsync_NeverIncludesTheActualValue_AnywhereInTheOutput()
    {
        var json = $$"""{ "ConnectionStrings": { "Default": "{{SecretSentinel}}" } }""";
        var path = WriteFile("appsettings.json", json);

        var documents = await CollectAsync(path);

        foreach (var document in documents)
        {
            document.Symbol.Name.ShouldNotContain(SecretSentinel);
            document.Symbol.QualifiedName.ShouldNotContain(SecretSentinel);
            document.Symbol.CanonicalName.ShouldNotContain(SecretSentinel);
            document.Id.ShouldNotContain(SecretSentinel);
            (document.EmbeddingText ?? string.Empty).ShouldNotContain(SecretSentinel);
        }
    }

    [Fact]
    public async Task AnalyzeAsync_IsDeterministic_AcrossRuns()
    {
        var path = WriteFile("appsettings.json", """{ "A": { "B": 1 }, "C": [1, 2] }""");

        var first = await CollectAsync(path);
        var second = await CollectAsync(path);

        first.Select(d => d.Id).ShouldBe(second.Select(d => d.Id));
    }

    [Fact]
    public async Task AnalyzeAsync_DisambiguatesIdenticalKeyPaths_AcrossSiblingFiles()
    {
        var basePath = WriteFile("appsettings.json", """{ "Shared": "x" }""");
        var envPath = WriteFile("appsettings.Development.json", """{ "Shared": "y" }""");

        var baseDocuments = await CollectAsync(basePath);
        var envDocuments = await CollectAsync(envPath);

        var baseKey = baseDocuments.Single(d => d.Kind == CiirKind.ConfigurationKey);
        var envKey = envDocuments.Single(d => d.Kind == CiirKind.ConfigurationKey);

        baseKey.Id.ShouldNotBe(envKey.Id);
    }

    public void Dispose()
    {
        if (Directory.Exists(rootDirectory))
        {
            Directory.Delete(rootDirectory, recursive: true);
        }
    }

    private static async Task<List<CiirDocument>> CollectAsync((string Full, string RelativeName) path)
    {
        var documents = new List<CiirDocument>();
        await foreach (var document in AppSettingsAnalyzer.AnalyzeAsync(path.Full, Path.GetDirectoryName(path.Full)!, TestContext.Current.CancellationToken))
        {
            documents.Add(document);
        }

        return documents;
    }

    private (string Full, string RelativeName) WriteFile(string fileName, string content)
    {
        var fullPath = Path.Combine(rootDirectory, fileName);
        File.WriteAllText(fullPath, content);
        return (fullPath, fileName);
    }
}
