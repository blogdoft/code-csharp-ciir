using System.Reflection;

namespace Ciir.Application.Model;

/// <summary>
/// The version of the generator (the <c>ciir</c> tool) that produced an analysis, reported as
/// <c>generator.version</c> in <c>manifest.json</c>. Every assembly of the solution shares the
/// package version, so this reads the informational version stamped at build time.
/// </summary>
public static class GeneratorVersion
{
    private const string Fallback = "0.0.0";

    /// <summary>Gets the version of the running generator, without build metadata (<c>+&lt;sha&gt;</c>).</summary>
    public static string Current { get; } = Normalize(
        typeof(GeneratorVersion).Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion);

    /// <summary>
    /// Strips the SemVer build metadata (<c>+&lt;sha&gt;</c>) from <paramref name="informationalVersion"/>,
    /// keeping any pre-release label, and falls back to <c>0.0.0</c> when there is nothing usable.
    /// </summary>
    /// <param name="informationalVersion">The raw <c>AssemblyInformationalVersion</c> value, if any.</param>
    public static string Normalize(string? informationalVersion)
    {
        if (string.IsNullOrWhiteSpace(informationalVersion))
        {
            return Fallback;
        }

        var metadataIndex = informationalVersion.IndexOf('+', StringComparison.Ordinal);
        var version = (metadataIndex >= 0 ? informationalVersion[..metadataIndex] : informationalVersion).Trim();

        return version.Length == 0 ? Fallback : version;
    }
}
