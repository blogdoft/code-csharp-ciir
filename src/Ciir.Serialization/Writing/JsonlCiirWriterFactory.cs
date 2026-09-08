using Ciir.Application.Ports;

namespace Ciir.Serialization.Writing;

/// <summary>Creates a <see cref="JsonlCiirWriter"/> writing <c>ciir.jsonl</c> under the given output directory.</summary>
public sealed class JsonlCiirWriterFactory : ICiirWriterFactory
{
    /// <inheritdoc />
    public ICiirWriter Create(string outputDirectory)
    {
        ArgumentNullException.ThrowIfNull(outputDirectory);

        return JsonlCiirWriter.CreateForFile(Path.Combine(outputDirectory, "ciir.jsonl"));
    }
}
