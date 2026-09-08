using Ciir.Core;

namespace Ciir.Application.Ports;

/// <summary>
/// Streams <see cref="CiirDocument"/> records to the CIIR output as they are produced, so the
/// application never needs to hold the full analysis result in memory. Implemented by an adapter
/// (e.g. a JSONL writer) that Application depends on only through this abstraction.
/// </summary>
public interface ICiirWriter : IAsyncDisposable
{
    /// <summary>Writes a single document to the output.</summary>
    /// <param name="document">The document to write.</param>
    /// <param name="cancellationToken">A token to observe for cancellation.</param>
    Task WriteAsync(CiirDocument document, CancellationToken cancellationToken);
}
