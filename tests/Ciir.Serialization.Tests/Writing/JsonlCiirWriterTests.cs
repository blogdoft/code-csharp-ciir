using Ciir.Core;
using Ciir.Core.Identity;
using Ciir.Core.Symbols;
using Ciir.Serialization.Writing;
using Json.Schema;
using Shouldly;
using System.Text;
using System.Text.Json;

namespace Ciir.Serialization.Tests.Writing;

public class JsonlCiirWriterTests
{
    [Fact]
    public async Task WriteAsync_WritesExactlyOneJsonLinePerDocument()
    {
        await using var stream = new MemoryStream();
        await using (var writer = new JsonlCiirWriter(stream, leaveOpen: true))
        {
            await writer.WriteAsync(BuildTypeDocument("Order"), TestContext.Current.CancellationToken);
            await writer.WriteAsync(BuildTypeDocument("Payment"), TestContext.Current.CancellationToken);
        }

        var content = Encoding.UTF8.GetString(stream.ToArray());
        var lines = content.Split('\n', StringSplitOptions.RemoveEmptyEntries);

        lines.Length.ShouldBe(2);
        foreach (var line in lines)
        {
            Should.NotThrow(() => JsonDocument.Parse(line));
        }
    }

    [Fact]
    public async Task WriteAsync_OmitsEmptyCollectionsAndNullProperties()
    {
        await using var stream = new MemoryStream();
        await using (var writer = new JsonlCiirWriter(stream, leaveOpen: true))
        {
            await writer.WriteAsync(BuildTypeDocument("Order"), TestContext.Current.CancellationToken);
        }

        var json = Encoding.UTF8.GetString(stream.ToArray());

        json.ShouldNotContain("\"comments\"");
        json.ShouldNotContain("\"relations\"");
        json.ShouldNotContain("\"conditions\"");
        json.ShouldNotContain("\"additionalSourceLocations\"");
        json.ShouldNotContain("\"documentation\"");
        json.ShouldNotContain("\"source\"");
        json.ShouldNotContain("\"extensions\"");
    }

    [Fact]
    public async Task WriteAsync_UsesCamelCasePropertyNamesAndSnakeCaseEnumTokens()
    {
        await using var stream = new MemoryStream();
        await using (var writer = new JsonlCiirWriter(stream, leaveOpen: true))
        {
            await writer.WriteAsync(BuildTypeDocument("Order"), TestContext.Current.CancellationToken);
        }

        var json = Encoding.UTF8.GetString(stream.ToArray());

        json.ShouldContain("\"schemaVersion\"");
        json.ShouldContain("\"typeKind\":\"class\"");
        json.ShouldContain("\"accessibility\":\"public\"");
    }

    [Fact]
    public async Task WriteAsync_ProducesOutputValidAgainstCiirSchema()
    {
        await using var stream = new MemoryStream();
        await using (var writer = new JsonlCiirWriter(stream, leaveOpen: true))
        {
            await writer.WriteAsync(BuildTypeDocument("Order"), TestContext.Current.CancellationToken);
        }

        var json = Encoding.UTF8.GetString(stream.ToArray()).TrimEnd('\n');
        var instance = JsonDocument.Parse(json).RootElement;

        var results = CiirSchemaFixture.Schema.Evaluate(instance, new EvaluationOptions { OutputFormat = OutputFormat.List });

        results.IsValid.ShouldBeTrue();
    }

    [Fact]
    public async Task WriteAsync_IsDeterministic_AcrossRepeatedWrites()
    {
        async Task<string> WriteOnceAsync()
        {
            await using var stream = new MemoryStream();
            await using (var writer = new JsonlCiirWriter(stream, leaveOpen: true))
            {
                await writer.WriteAsync(BuildTypeDocument("Order"), TestContext.Current.CancellationToken);
            }

            return Encoding.UTF8.GetString(stream.ToArray());
        }

        var first = await WriteOnceAsync();
        var second = await WriteOnceAsync();

        first.ShouldBe(second);
    }

    private static CiirDocument BuildTypeDocument(string typeName)
    {
        var qualifiedName = $"Payments.Domain.{typeName}";
        var id = CiirIdentity.ComputeId("csharp", "Payments.Domain", CiirKind.Type, qualifiedName);

        return new CiirDocument
        {
            Id = id,
            Kind = CiirKind.Type,
            Language = "csharp",
            Project = "Payments.Domain",
            Symbol = new CiirSymbol { Name = typeName, QualifiedName = qualifiedName, CanonicalName = qualifiedName },
            Type = new CiirTypeInfo { TypeKind = CiirTypeKind.Class, Accessibility = CiirAccessibility.Public },
        };
    }
}
