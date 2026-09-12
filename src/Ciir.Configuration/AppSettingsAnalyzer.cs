using Ciir.Configuration.EmbeddingText;
using Ciir.Core;
using Ciir.Core.Configuration;
using Ciir.Core.EmbeddingText;
using Ciir.Core.Hashing;
using Ciir.Core.Identity;
using Ciir.Core.Source;
using Ciir.Core.Symbols;
using System.Runtime.CompilerServices;
using System.Text.Json;

namespace Ciir.Configuration;

/// <summary>
/// Parses an <c>appsettings*.json</c> file into a <c>configuration</c> document for the file
/// itself plus one <c>configuration_key</c> document per flattened key path (leaves and
/// object/array containers alike). Only the key path and the value's JSON type are ever captured
/// — the value itself is never read or serialized, since appsettings files commonly hold secrets
/// (connection strings, API keys) that must not leak into CIIR output.
/// </summary>
internal static class AppSettingsAnalyzer
{
    private const string Language = "json";
    private const string ProjectName = "Configuration";
    private static readonly IEmbeddingTextPolicy Policy = new AllowAllEmbeddingTextPolicy();

    public static async IAsyncEnumerable<CiirDocument> AnalyzeAsync(
        string filePath,
        string rootDirectory,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var bytes = await File.ReadAllBytesAsync(filePath, cancellationToken);
        var relativePath = RelativePath.From(rootDirectory, filePath);

        using var jsonDocument = JsonDocument.Parse(bytes);

        yield return AttachEmbeddingText(new CiirDocument
        {
            Id = CiirIdentity.ComputeId(Language, ProjectName, CiirKind.Configuration, relativePath),
            Kind = CiirKind.Configuration,
            Language = Language,
            Project = ProjectName,
            Symbol = new CiirSymbol
            {
                Name = Path.GetFileName(relativePath),
                QualifiedName = relativePath,
                CanonicalName = relativePath,
            },
            Source = new CiirSourceLocation
            {
                Path = relativePath,
                StartLine = 1,
                StartColumn = 1,
                EndLine = 1,
                EndColumn = 1,
                Hash = Sha256Text.ComputePrefixedHash(bytes),
            },
        });

        foreach (var document in WalkKeys(jsonDocument.RootElement, relativePath, keyPath: null))
        {
            yield return AttachEmbeddingText(document);
        }
    }

    private static IEnumerable<CiirDocument> WalkKeys(JsonElement element, string relativePath, string? keyPath)
    {
        if (keyPath is not null)
        {
            yield return BuildKeyDocument(element, relativePath, keyPath);
        }

        if (element.ValueKind == JsonValueKind.Object)
        {
            var properties = element.EnumerateObject().OrderBy(property => property.Name, StringComparer.Ordinal);
            foreach (var property in properties)
            {
                var childKeyPath = keyPath is null ? property.Name : $"{keyPath}:{property.Name}";
                foreach (var document in WalkKeys(property.Value, relativePath, childKeyPath))
                {
                    yield return document;
                }
            }
        }
        else if (element.ValueKind == JsonValueKind.Array)
        {
            var index = 0;
            foreach (var item in element.EnumerateArray())
            {
                var childKeyPath = keyPath is null ? index.ToString(System.Globalization.CultureInfo.InvariantCulture) : $"{keyPath}:{index}";
                foreach (var document in WalkKeys(item, relativePath, childKeyPath))
                {
                    yield return document;
                }

                index++;
            }
        }
    }

    private static CiirDocument BuildKeyDocument(JsonElement element, string relativePath, string keyPath)
    {
        var lastSeparator = keyPath.LastIndexOf(':');
        var name = lastSeparator < 0 ? keyPath : keyPath[(lastSeparator + 1)..];
        var qualifiedName = $"{relativePath}:{keyPath}";
        var canonicalName = $"{relativePath}#{keyPath}";

        return new CiirDocument
        {
            Id = CiirIdentity.ComputeId(Language, ProjectName, CiirKind.ConfigurationKey, canonicalName),
            Kind = CiirKind.ConfigurationKey,
            Language = Language,
            Project = ProjectName,
            Symbol = new CiirSymbol
            {
                Name = name,
                QualifiedName = qualifiedName,
                CanonicalName = canonicalName,
                Container = relativePath,
            },
            ConfigurationKey = new CiirConfigurationKeyInfo { ValueType = ToValueKind(element.ValueKind) },
        };
    }

    private static CiirConfigurationValueKind ToValueKind(JsonValueKind valueKind) => valueKind switch
    {
        JsonValueKind.Object => CiirConfigurationValueKind.ObjectValue,
        JsonValueKind.Array => CiirConfigurationValueKind.ArrayValue,
        JsonValueKind.String => CiirConfigurationValueKind.StringValue,
        JsonValueKind.Number => CiirConfigurationValueKind.NumberValue,
        JsonValueKind.True or JsonValueKind.False => CiirConfigurationValueKind.BooleanValue,
        JsonValueKind.Null => CiirConfigurationValueKind.NullValue,
        _ => throw new ArgumentOutOfRangeException(nameof(valueKind), valueKind, message: null),
    };

    private static CiirDocument AttachEmbeddingText(CiirDocument document)
    {
        var embeddingText = EmbeddingTextBuilder.Build(document, Policy);
        return document with { EmbeddingText = embeddingText, EmbeddingTextHash = Sha256Text.ComputePrefixedHash(embeddingText) };
    }
}
