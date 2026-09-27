using BlogDoFT.Libs.ResultPattern;
using Ciir.Application.Model;

namespace Ciir.Application.Ports;

/// <summary>Sends a generated <c>ciir.jsonl</c> file to the code-ciir-indexer.</summary>
public interface ICiirUploader
{
    /// <summary>Uploads <paramref name="filePath"/> as described by <paramref name="options"/>.</summary>
    /// <param name="filePath">The <c>ciir.jsonl</c> file to send.</param>
    /// <param name="options">The destination, project and credentials.</param>
    /// <param name="cancellationToken">A token to observe for cancellation.</param>
    /// <returns>The indexer's receipt, or a failure describing why the upload did not happen.</returns>
    Task<Result<CiirUploadReceipt>> UploadAsync(string filePath, SendOptions options, CancellationToken cancellationToken);
}
