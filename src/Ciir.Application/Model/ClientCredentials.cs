namespace Ciir.Application.Model;

/// <summary>Authenticates by negotiating a token through the OAuth2 <c>client_credentials</c> grant.</summary>
/// <param name="ClientId">The Keycloak client id.</param>
/// <param name="ClientSecret">The Keycloak client secret.</param>
public sealed record ClientCredentials(string ClientId, string ClientSecret) : IndexerCredentials
{
    /// <inheritdoc />
    public override string ToString() => $"{nameof(ClientCredentials)} {{ ClientId = {ClientId} }}";
}
