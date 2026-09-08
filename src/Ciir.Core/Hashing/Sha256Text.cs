using System.Security.Cryptography;
using System.Text;

namespace Ciir.Core.Hashing;

/// <summary>
/// Single shared SHA-256 utility reused wherever the CIIR contract needs a deterministic
/// content hash (document <c>id</c>, <c>source.hash</c>, <c>embeddingTextHash</c>, manifest file hashes).
/// </summary>
public static class Sha256Text
{
    /// <summary>Computes the lowercase hex SHA-256 digest of <paramref name="text"/>.</summary>
    /// <param name="text">The text to hash.</param>
    public static string ComputeHash(string text)
    {
        ArgumentNullException.ThrowIfNull(text);

        return ComputeHash(Encoding.UTF8.GetBytes(text));
    }

    /// <summary>Computes the lowercase hex SHA-256 digest of <paramref name="bytes"/>.</summary>
    /// <param name="bytes">The bytes to hash.</param>
    public static string ComputeHash(byte[] bytes)
    {
        ArgumentNullException.ThrowIfNull(bytes);

        return Convert.ToHexStringLower(SHA256.HashData(bytes));
    }

    /// <summary>Computes the <c>sha256:&lt;hex&gt;</c>-prefixed digest of <paramref name="text"/>.</summary>
    /// <param name="text">The text to hash.</param>
    public static string ComputePrefixedHash(string text) => $"sha256:{ComputeHash(text)}";

    /// <summary>Computes the <c>sha256:&lt;hex&gt;</c>-prefixed digest of <paramref name="bytes"/>.</summary>
    /// <param name="bytes">The bytes to hash.</param>
    public static string ComputePrefixedHash(byte[] bytes) => $"sha256:{ComputeHash(bytes)}";
}
