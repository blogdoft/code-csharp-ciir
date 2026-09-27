using BlogDoFT.Libs.ResultPattern;
using Ciir.Application.Model;
using Ciir.Application.Ports;
using System.Net.Http.Headers;
using System.Text.Json;

namespace Ciir.Indexer.Client;

/// <summary>
/// <see cref="ICiirUploader"/> over HTTP: authenticates as configured, then streams the file to the
/// indexer's <c>POST api/indexer/ciir-uploads</c> as <c>multipart/form-data</c>.
/// </summary>
public sealed class HttpCiirUploader : ICiirUploader
{
    private static readonly JsonSerializerOptions ResponseJson = new(JsonSerializerDefaults.Web);

    private readonly HttpClient httpClient;
    private readonly IndexerTokenProvider tokenProvider;

    /// <summary>Initializes a new instance of the <see cref="HttpCiirUploader"/> class.</summary>
    /// <param name="httpClient">The client used for every request; its timeout bounds the (potentially large) upload.</param>
    public HttpCiirUploader(HttpClient httpClient)
    {
        ArgumentNullException.ThrowIfNull(httpClient);

        this.httpClient = httpClient;
        tokenProvider = new IndexerTokenProvider(httpClient);
    }

    /// <inheritdoc />
    public async Task<Result<CiirUploadReceipt>> UploadAsync(string filePath, SendOptions options, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(filePath);
        ArgumentNullException.ThrowIfNull(options);

        if (!File.Exists(filePath))
        {
            return Fail("file_not_found", $"Cannot send '{filePath}': the file does not exist.");
        }

        var bearerToken = await ResolveBearerTokenAsync(options, cancellationToken);
        if (bearerToken.IsFailure)
        {
            return Result<CiirUploadReceipt>.FromFailure(bearerToken.Failure);
        }

        var endpoint = IndexerEndpoints.Upload(options.BaseUrl);
        try
        {
            await using var file = File.OpenRead(filePath);
            using var content = new MultipartFormDataContent
            {
                // The indexer validates projectId before storing any byte of the file, so it must come first.
                { new StringContent(options.ProjectId.ToString("D")), "projectId" },
            };

            var fileContent = new StreamContent(file);
            fileContent.Headers.ContentType = new MediaTypeHeaderValue("application/x-ndjson");
            content.Add(fileContent, "ciirFile", "ciir.jsonl");

            using var request = new HttpRequestMessage(HttpMethod.Post, endpoint) { Content = content };
            if (bearerToken.Value is { } token)
            {
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            }

            using var response = await httpClient.SendAsync(request, cancellationToken);
            return await ReadResponseAsync(response, endpoint, cancellationToken);
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException && !cancellationToken.IsCancellationRequested)
        {
            return Fail("upload_unreachable", $"Could not send the file to '{endpoint}': {ex.Message}");
        }
    }

    private static Result<CiirUploadReceipt> Fail(string code, string message) =>
        Result<CiirUploadReceipt>.FromFailure(new Failure(code, message));

    private static async Task<Result<CiirUploadReceipt>> ReadResponseAsync(HttpResponseMessage response, Uri endpoint, CancellationToken cancellationToken)
    {
        var body = await response.Content.ReadAsStringAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            return Fail("upload_rejected", $"The indexer rejected the upload to '{endpoint}': {DescribeRejection(response, body)}");
        }

        try
        {
            var receipt = JsonSerializer.Deserialize<UploadResponse>(body, ResponseJson);
            return receipt is { UploadId: var uploadId } && uploadId != Guid.Empty
                ? Result<CiirUploadReceipt>.FromSuccess(new CiirUploadReceipt(uploadId, receipt.Status ?? string.Empty))
                : Fail("invalid_response", $"The indexer accepted the upload but its response had no uploadId: {body}");
        }
        catch (JsonException)
        {
            return Fail("invalid_response", $"The indexer accepted the upload but its response was not valid JSON: {body}");
        }
    }

    private static string DescribeRejection(HttpResponseMessage response, string body)
    {
        var status = (int)response.StatusCode;
        var reason = status switch
        {
            401 => "HTTP 401 — the token is missing, expired or invalid; check --token or --clientId/--clientSecret.",
            404 => "HTTP 404 — the project was not found; check --projectId.",
            413 => "HTTP 413 — the file exceeds the indexer's maximum upload size.",
            429 => "HTTP 429 — the indexer's concurrent-upload limit was reached; retry later.",
            _ => $"HTTP {status}.",
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

    private async Task<Result<string?>> ResolveBearerTokenAsync(SendOptions options, CancellationToken cancellationToken)
    {
        switch (options.Credentials)
        {
            case BearerTokenCredentials bearer:
                return Result<string?>.FromSuccess(bearer.Token);
            case ClientCredentials clientCredentials:
                var token = await tokenProvider.AcquireAsync(options.BaseUrl, clientCredentials, cancellationToken);
                return token.IsFailure
                    ? Result<string?>.FromFailure(token.Failure)
                    : Result<string?>.FromSuccess(token.Value);
            default:
                return Result<string?>.FromSuccess(null);
        }
    }

    private sealed record UploadResponse(Guid UploadId, string? Status);

    private sealed record ProblemResponse(string? Detail);
}
