using Ciir.Core;
using Ciir.Core.EmbeddingText;
using Ciir.Core.Hashing;

namespace Ciir.CSharp.EmbeddingText;

/// <summary>Computes and attaches <c>embeddingText</c>/<c>embeddingTextHash</c> to a fully-populated document.</summary>
internal static class EmbeddingTextAttacher
{
    private static readonly IEmbeddingTextPolicy Policy = new DotNetNoiseEmbeddingTextPolicy();

    public static CiirDocument Attach(CiirDocument document)
    {
        var text = EmbeddingTextBuilder.Build(document, Policy);
        return document with { EmbeddingText = text, EmbeddingTextHash = Sha256Text.ComputePrefixedHash(text) };
    }
}
