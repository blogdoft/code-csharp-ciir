namespace Ciir.Application.Model;

/// <summary>Authenticates with a ready-made access token, sent as <c>Authorization: Bearer</c>.</summary>
/// <param name="Token">The access token.</param>
public sealed record BearerTokenCredentials(string Token) : IndexerCredentials
{
    /// <inheritdoc />
    public override string ToString() => nameof(BearerTokenCredentials);
}
