namespace Ciir.Application.Model;

/// <summary>What the code-ciir-indexer acknowledged after accepting an upload.</summary>
/// <param name="UploadId">The id of the pending upload, usable to poll its progress in the indexer.</param>
/// <param name="Status">The upload's status right after creation (always <c>pending</c> today).</param>
public sealed record CiirUploadReceipt(Guid UploadId, string Status);
