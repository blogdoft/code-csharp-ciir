using Ciir.Application.Ports;
using Ciir.Core;
using Ciir.Serialization.Json;
using System.Text;
using System.Text.Json;

namespace Ciir.Serialization.Writing;

/// <summary>
/// Writes CIIR documents as JSON Lines: exactly one JSON object per line, streamed as each
/// document is produced. Never buffers the full result in memory or emits a JSON array.
/// </summary>
public sealed class JsonlCiirWriter : ICiirWriter
{
    private readonly StreamWriter writer;
    private readonly JsonSerializerOptions options;
    private bool disposed;

    /// <summary>Initializes a new instance of the <see cref="JsonlCiirWriter"/> class over an already-open stream (e.g. a <see cref="MemoryStream"/> in tests).</summary>
    /// <param name="outputStream">The stream to write JSON Lines to.</param>
    /// <param name="leaveOpen">Whether to leave <paramref name="outputStream"/> open when this writer is disposed.</param>
    public JsonlCiirWriter(Stream outputStream, bool leaveOpen = false)
    {
        ArgumentNullException.ThrowIfNull(outputStream);

        writer = new StreamWriter(outputStream, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false), leaveOpen: leaveOpen)
        {
            NewLine = "\n",
        };
        options = CiirJsonSerializerOptions.Create();
    }

    /// <summary>Creates a writer that (re)creates and writes to the file at <paramref name="outputFilePath"/>.</summary>
    /// <param name="outputFilePath">The destination file path (e.g. <c>ciir.jsonl</c>).</param>
    public static JsonlCiirWriter CreateForFile(string outputFilePath)
    {
        ArgumentNullException.ThrowIfNull(outputFilePath);

        var stream = new FileStream(outputFilePath, FileMode.Create, FileAccess.Write, FileShare.None);
        return new JsonlCiirWriter(stream);
    }

    /// <inheritdoc />
    public async Task WriteAsync(CiirDocument document, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(document);

        var json = JsonSerializer.Serialize(document, options);
        await writer.WriteLineAsync(json.AsMemory(), cancellationToken);
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        if (disposed)
        {
            return;
        }

        disposed = true;
        await writer.FlushAsync();
        await writer.DisposeAsync();
    }
}
