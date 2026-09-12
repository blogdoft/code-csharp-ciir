using Ciir.Configuration.EmbeddingText;
using Ciir.Core;
using Ciir.Core.Configuration;
using Ciir.Core.EmbeddingText;
using Ciir.Core.Hashing;
using Ciir.Core.Identity;
using Ciir.Core.Source;
using Ciir.Core.Symbols;

namespace Ciir.Configuration;

/// <summary>
/// Captures a YAML file as file-level metadata only. YAML content is never structurally parsed:
/// its purpose varies too widely (CI workflows, docker-compose, Kubernetes manifests, ...) to
/// model with a single schema, so only path/hash/size are recorded.
/// </summary>
internal static class YamlFileAnalyzer
{
    private const string Language = "yaml";
    private const string ProjectName = "Configuration";
    private static readonly IEmbeddingTextPolicy Policy = new AllowAllEmbeddingTextPolicy();

    public static async Task<CiirDocument> AnalyzeAsync(string filePath, string rootDirectory, CancellationToken cancellationToken)
    {
        var bytes = await File.ReadAllBytesAsync(filePath, cancellationToken);
        var relativePath = RelativePath.From(rootDirectory, filePath);

        var document = new CiirDocument
        {
            Id = CiirIdentity.ComputeId(Language, ProjectName, CiirKind.File, relativePath),
            Kind = CiirKind.File,
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
            File = new CiirFileInfo { SizeBytes = bytes.LongLength },
        };

        var embeddingText = EmbeddingTextBuilder.Build(document, Policy);
        return document with { EmbeddingText = embeddingText, EmbeddingTextHash = Sha256Text.ComputePrefixedHash(embeddingText) };
    }
}
