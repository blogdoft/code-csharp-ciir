using System.Reflection;

namespace Ciir.Serialization.SchemaProvider;

/// <summary>
/// Exposes the <c>ciir.schema.json</c> contract embedded in this assembly, so it can be copied
/// to the analysis output directory and reused by conformance tests without depending on a
/// runtime file-system path to the repository's <c>schemas/</c> directory.
/// </summary>
public static class CiirSchemaProvider
{
    private const string ResourceName = "Ciir.Serialization.ciir.schema.json";

    /// <summary>Reads the embedded <c>ciir.schema.json</c> contents.</summary>
    public static string GetSchemaJson()
    {
        var assembly = Assembly.GetExecutingAssembly();
        using var stream = assembly.GetManifestResourceStream(ResourceName)
            ?? throw new InvalidOperationException($"Embedded resource '{ResourceName}' was not found in {assembly.FullName}.");
        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }
}
