using BlogDoFT.Libs.ResultPattern;
using Ciir.Application.Model;

namespace Ciir.Cli.Composition;

/// <summary>Turns the raw <c>--send</c> arguments (and <c>CIIR_BASE_URL</c>) into validated <see cref="SendOptions"/>.</summary>
internal static class SendOptionsFactory
{
    public const string BaseUrlEnvironmentVariable = "CIIR_BASE_URL";

    /// <summary>Builds the send options, failing when the base URL or project id is missing or invalid.</summary>
    /// <param name="baseUrlArgument">The value given to <c>--send</c>, if any.</param>
    /// <param name="baseUrlFromEnvironment">The value of <c>CIIR_BASE_URL</c>, used when <paramref name="baseUrlArgument"/> is empty.</param>
    /// <param name="projectId">The value of <c>--projectId</c>.</param>
    /// <param name="token">The value of <c>--token</c>.</param>
    /// <param name="clientId">The value of <c>--clientId</c>.</param>
    /// <param name="clientSecret">The value of <c>--clientSecret</c>.</param>
    public static Result<SendOptions> Create(
        string? baseUrlArgument,
        string? baseUrlFromEnvironment,
        string? projectId,
        string? token,
        string? clientId,
        string? clientSecret)
    {
        var baseUrl = ResolveBaseUrl(baseUrlArgument, baseUrlFromEnvironment);
        if (baseUrl.IsFailure)
        {
            return Result<SendOptions>.FromFailure(baseUrl.Failure);
        }

        if (!Guid.TryParse(projectId, out var parsedProjectId) || parsedProjectId == Guid.Empty)
        {
            var message = string.IsNullOrWhiteSpace(projectId)
                ? "--projectId is required with --send: it is the id of the project registered in the indexer."
                : $"--projectId '{projectId}' is not a valid project id (expected a GUID).";
            return Result<SendOptions>.FromFailure(new Failure("project_id_invalid", message));
        }

        return Result<SendOptions>.FromSuccess(new SendOptions
        {
            BaseUrl = baseUrl.Value,
            ProjectId = parsedProjectId,
            Credentials = ResolveCredentials(token, clientId, clientSecret),
        });
    }

    /// <summary>Whether exactly one of the client credentials was given (and no token overrides them), which silently disables them.</summary>
    /// <param name="token">The value of <c>--token</c>.</param>
    /// <param name="clientId">The value of <c>--clientId</c>.</param>
    /// <param name="clientSecret">The value of <c>--clientSecret</c>.</param>
    public static bool HasIncompleteClientCredentials(string? token, string? clientId, string? clientSecret) =>
        string.IsNullOrWhiteSpace(token) && string.IsNullOrWhiteSpace(clientId) != string.IsNullOrWhiteSpace(clientSecret);

    private static IndexerCredentials? ResolveCredentials(string? token, string? clientId, string? clientSecret)
    {
        if (!string.IsNullOrWhiteSpace(token))
        {
            return new BearerTokenCredentials(token.Trim());
        }

        return !string.IsNullOrWhiteSpace(clientId) && !string.IsNullOrWhiteSpace(clientSecret)
            ? new ClientCredentials(clientId.Trim(), clientSecret)
            : null;
    }

    private static bool IsHttp(Uri uri) =>
        string.Equals(uri.Scheme, Uri.UriSchemeHttp, StringComparison.Ordinal) || string.Equals(uri.Scheme, Uri.UriSchemeHttps, StringComparison.Ordinal);

    private static Result<Uri> ResolveBaseUrl(string? argument, string? fromEnvironment)
    {
        var value = string.IsNullOrWhiteSpace(argument) ? fromEnvironment : argument;
        if (string.IsNullOrWhiteSpace(value))
        {
            return Result<Uri>.FromFailure(new Failure(
                "base_url_missing",
                $"The {BaseUrlEnvironmentVariable} environment variable is not set, so the base URL must be provided: ciir <path> --send <base-url>."));
        }

        if (!Uri.TryCreate(value.Trim(), UriKind.Absolute, out var uri) || !IsHttp(uri))
        {
            return Result<Uri>.FromFailure(new Failure("base_url_invalid", $"The base URL '{value}' is not a valid http/https URL."));
        }

        // Relative endpoints only resolve under a path prefix (e.g. /code-brain) when the base ends with a slash.
        return Result<Uri>.FromSuccess(new UriBuilder(uri) { Path = uri.AbsolutePath.TrimEnd('/') + "/", Query = string.Empty, Fragment = string.Empty }.Uri);
    }
}
