using Ciir.Application.Model;
using Ciir.Application.Ports;
using Ciir.Core;
using System.Runtime.CompilerServices;

namespace Ciir.Configuration;

/// <summary>
/// Captures <c>appsettings*.json</c> files (parsed into <c>configuration</c>/<c>configuration_key</c>
/// documents) and <c>*.yaml</c>/<c>*.yml</c> files (captured only as <c>file</c> metadata) found
/// anywhere under the analyzed root. Never depends on Roslyn/MSBuild.
/// </summary>
public sealed class ConfigurationCodeAnalyzer : ICodeAnalyzer
{
    /// <inheritdoc />
    public bool CanAnalyze(string projectPath) => IsAppSettingsJson(projectPath) || IsYaml(projectPath);

    /// <inheritdoc />
    public async IAsyncEnumerable<CiirDocument> AnalyzeAsync(
        string projectPath,
        string rootDirectory,
        AnalysisOptions options,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(projectPath);
        ArgumentNullException.ThrowIfNull(rootDirectory);
        ArgumentNullException.ThrowIfNull(options);

        if (IsYaml(projectPath))
        {
            yield return await YamlFileAnalyzer.AnalyzeAsync(projectPath, rootDirectory, cancellationToken);
            yield break;
        }

        await foreach (var document in AppSettingsAnalyzer.AnalyzeAsync(projectPath, rootDirectory, cancellationToken))
        {
            yield return document;
        }
    }

    private static bool IsAppSettingsJson(string path) =>
        Path.GetFileNameWithoutExtension(path).StartsWith("appsettings", StringComparison.OrdinalIgnoreCase)
        && string.Equals(Path.GetExtension(path), ".json", StringComparison.OrdinalIgnoreCase);

    private static bool IsYaml(string path) =>
        string.Equals(Path.GetExtension(path), ".yaml", StringComparison.OrdinalIgnoreCase)
        || string.Equals(Path.GetExtension(path), ".yml", StringComparison.OrdinalIgnoreCase);
}
