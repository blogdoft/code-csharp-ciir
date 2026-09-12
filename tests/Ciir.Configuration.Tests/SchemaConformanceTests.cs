using Ciir.Core;
using Ciir.Serialization.Json;
using Json.Schema;
using Shouldly;
using System.Text.Json;

namespace Ciir.Configuration.Tests;

public class SchemaConformanceTests : IDisposable
{
    private readonly string rootDirectory = Path.Combine(Path.GetTempPath(), "ciir-configuration-schema-tests-" + Guid.NewGuid());

    public SchemaConformanceTests()
    {
        Directory.CreateDirectory(rootDirectory);
    }

    [Fact]
    public async Task AppSettingsAnalyzer_ProducesSchemaValidDocuments()
    {
        const string json = """
            {
              "ConnectionStrings": { "Default": "value" },
              "Feature": { "Enabled": true, "Retries": 3, "Tags": ["a", "b"] },
              "Nothing": null
            }
            """;
        var path = Path.Combine(rootDirectory, "appsettings.json");
        File.WriteAllText(path, json);

        var documents = new List<CiirDocument>();
        await foreach (var document in AppSettingsAnalyzer.AnalyzeAsync(path, rootDirectory, TestContext.Current.CancellationToken))
        {
            documents.Add(document);
        }

        AssertAllDocumentsAreSchemaValid(documents);
    }

    [Fact]
    public async Task YamlFileAnalyzer_ProducesASchemaValidDocument()
    {
        var path = Path.Combine(rootDirectory, "docker-compose.yml");
        File.WriteAllText(path, "version: '3.8'\n");

        var document = await YamlFileAnalyzer.AnalyzeAsync(path, rootDirectory, TestContext.Current.CancellationToken);

        AssertAllDocumentsAreSchemaValid([document]);
    }

    public void Dispose()
    {
        if (Directory.Exists(rootDirectory))
        {
            Directory.Delete(rootDirectory, recursive: true);
        }
    }

    private static void AssertAllDocumentsAreSchemaValid(IEnumerable<CiirDocument> documents)
    {
        var jsonOptions = CiirJsonSerializerOptions.Create();

        foreach (var document in documents)
        {
            var json = JsonSerializer.Serialize(document, jsonOptions);
            var instance = JsonDocument.Parse(json).RootElement;
            var results = CiirSchemaFixture.Schema.Evaluate(instance, new EvaluationOptions { OutputFormat = OutputFormat.List });

            results.IsValid.ShouldBeTrue($"{document.Kind} '{document.Symbol.QualifiedName}' failed schema validation:\n{json}");
        }
    }
}
