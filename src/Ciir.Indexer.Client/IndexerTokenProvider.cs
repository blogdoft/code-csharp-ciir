using BlogDoFT.Libs.ResultPattern;
using Ciir.Application.Model;
using System.Net;
using System.Text;
using System.Text.Json;

namespace Ciir.Indexer.Client;

/// <summary>
/// Exchanges a client id and secret for an access token through the indexer's own token gateway
/// (<c>POST api/indexer/auth/token</c>), so this tool never needs to know how the token is issued.
/// </summary>
internal sealed class IndexerTokenProvider(HttpClient httpClient)
{
    private static readonly JsonSerializerOptions ResponseJson = new(JsonSerializerDefaults.Web);

    public async Task<Result<string>> AcquireAsync(Uri baseUrl, ClientCredentials credentials, CancellationToken cancellationToken)
    {
        var endpoint = IndexerEndpoints.Token(baseUrl);

        try
        {
            // A buffered body (Content-Length) rather than JsonContent's chunked stream: friendlier to ingresses and proxies.
            using var content = new StringContent(
                JsonSerializer.Serialize(new TokenRequest(credentials.ClientId, credentials.ClientSecret), ResponseJson),
                Encoding.UTF8,
                "application/json");
            using var response = await httpClient.PostAsync(endpoint, content, cancellationToken);
            var body = await response.Content.ReadAsStringAsync(cancellationToken);

            return response.IsSuccessStatusCode
                ? ReadToken(body)
                : Failed(DescribeRejection(response.StatusCode, body));
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException && !cancellationToken.IsCancellationRequested)
        {
            return Failed($"Could not reach the indexer's token endpoint '{endpoint}': {ex.Message}");
        }
    }

    private static Result<string> Failed(string message) => Result<string>.FromFailure(new Failure("token_request_failed", message));

    private static Result<string> ReadToken(string body)
    {
        try
        {
            return JsonSerializer.Deserialize<TokenResponse>(body, ResponseJson) is { AccessToken: { Length: > 0 } accessToken }
                ? Result<string>.FromSuccess(accessToken)
                : Failed("The indexer's token endpoint answered without an access token.");
        }
        catch (JsonException)
        {
            return Failed("The indexer's token endpoint answered with an invalid response.");
        }
    }

    private static string DescribeRejection(HttpStatusCode status, string body)
    {
        var reason = status switch
        {
            HttpStatusCode.Unauthorized => "The indexer rejected the client credentials (HTTP 401); check --clientId/--clientSecret.",
            HttpStatusCode.NotFound => "The indexer has no token endpoint (HTTP 404): authentication is probably turned off there. Omit --clientId/--clientSecret, or use --token.",
            HttpStatusCode.BadGateway => "The indexer could not obtain a token from its identity provider (HTTP 502); retry later.",
            _ => $"The indexer's token endpoint answered HTTP {(int)status}.",
        };

        return TryReadProblemDetail(body) is { } detail ? $"{reason} {detail}" : reason;
    }

    private static string? TryReadProblemDetail(string body)
    {
        try
        {
            return JsonSerializer.Deserialize<ProblemResponse>(body, ResponseJson)?.Detail;
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private sealed record TokenRequest(string ClientId, string ClientSecret);

    private sealed record TokenResponse(string? AccessToken);

    private sealed record ProblemResponse(string? Detail);
}
