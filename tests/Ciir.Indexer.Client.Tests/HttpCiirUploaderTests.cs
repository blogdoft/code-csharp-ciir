using Ciir.Application.Model;
using Shouldly;
using System.Net;

namespace Ciir.Indexer.Client.Tests;

public sealed class HttpCiirUploaderTests : IDisposable
{
    private const string BaseUrl = "https://indexer.example/code-brain/";
    private const string TokenEndpoint = "https://indexer.example/code-brain/api/indexer/auth/token";

    private static readonly Guid ProjectId = Guid.Parse("3f2b1c0e-5a4d-4e8b-9c1a-0d2e3f4a5b6c");
    private static readonly Guid UploadId = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee");
    private static readonly string AcceptedJson = $$"""{"uploadId":"{{UploadId}}","status":"pending"}""";

    private readonly string filePath = Path.Combine(Path.GetTempPath(), $"ciir-uploader-tests-{Guid.NewGuid()}.jsonl");

    public HttpCiirUploaderTests() => File.WriteAllText(filePath, "{\"kind\":\"type\"}\n");

    public void Dispose() => File.Delete(filePath);

    [Fact]
    public async Task UploadAsync_PostsMultipartWithProjectIdBeforeTheFile_AndReturnsTheReceipt()
    {
        var handler = new StubHttpMessageHandler(_ => StubHttpMessageHandler.Json(HttpStatusCode.Accepted, AcceptedJson));

        var result = await Uploader(handler).UploadAsync(filePath, Options(), TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldBe(new CiirUploadReceipt(UploadId, "pending"));
        var request = handler.Requests.ShouldHaveSingleItem();
        request.Method.ShouldBe(HttpMethod.Post);
        request.Uri.ToString().ShouldBe("https://indexer.example/code-brain/api/indexer/ciir-uploads");
        request.Body.ShouldNotBeNull();
        request.Body.IndexOf("name=projectId", StringComparison.Ordinal).ShouldBeGreaterThan(-1);
        request.Body.IndexOf("name=projectId", StringComparison.Ordinal)
            .ShouldBeLessThan(request.Body.IndexOf("name=ciirFile", StringComparison.Ordinal));
        request.Body.ShouldContain(ProjectId.ToString("D"));
        request.Body.ShouldContain("filename=ciir.jsonl");
        request.Body.ShouldContain("{\"kind\":\"type\"}");
    }

    [Fact]
    public async Task UploadAsync_SendsNoAuthorizationHeader_WhenThereAreNoCredentials()
    {
        var handler = new StubHttpMessageHandler(_ => StubHttpMessageHandler.Json(HttpStatusCode.Accepted, AcceptedJson));

        await Uploader(handler).UploadAsync(filePath, Options(), TestContext.Current.CancellationToken);

        handler.Requests.ShouldHaveSingleItem().Authorization.ShouldBeNull();
    }

    [Fact]
    public async Task UploadAsync_SendsTheBearerToken_WhenATokenIsProvided()
    {
        var handler = new StubHttpMessageHandler(_ => StubHttpMessageHandler.Json(HttpStatusCode.Accepted, AcceptedJson));

        await Uploader(handler).UploadAsync(filePath, Options(new BearerTokenCredentials("abc.def")), TestContext.Current.CancellationToken);

        handler.Requests.ShouldHaveSingleItem().Authorization.ShouldBe("Bearer abc.def");
    }

    [Fact]
    public async Task UploadAsync_ExchangesTheClientCredentialsForATokenAtTheIndexerGateway_ThenUploadsWithIt()
    {
        var handler = new StubHttpMessageHandler(request => request.Uri.ToString() switch
        {
            TokenEndpoint => StubHttpMessageHandler.Json(HttpStatusCode.OK, """{"accessToken":"negotiated","tokenType":"Bearer","expiresIn":300}"""),
            _ => StubHttpMessageHandler.Json(HttpStatusCode.Accepted, AcceptedJson),
        });

        var result = await Uploader(handler).UploadAsync(
            filePath, Options(new ClientCredentials("pipeline", "s3cret")), TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        handler.Requests.Count.ShouldBe(2);
        var tokenRequest = handler.Requests[0];
        tokenRequest.Method.ShouldBe(HttpMethod.Post);
        tokenRequest.Authorization.ShouldBeNull();
        tokenRequest.Body.ShouldBe("""{"clientId":"pipeline","clientSecret":"s3cret"}""");
        handler.Requests[1].Authorization.ShouldBe("Bearer negotiated");
    }

    [Theory]
    [InlineData(HttpStatusCode.Unauthorized, "check --clientId/--clientSecret")]
    [InlineData(HttpStatusCode.NotFound, "Omit --clientId/--clientSecret, or use --token")]
    [InlineData(HttpStatusCode.BadGateway, "identity provider")]
    [InlineData(HttpStatusCode.InternalServerError, "HTTP 500")]
    public async Task UploadAsync_Fails_WithAHelpfulMessage_WhenTheGatewayRefusesToIssueAToken(HttpStatusCode status, string expectedHint)
    {
        var handler = new StubHttpMessageHandler(_ => new HttpResponseMessage(status));

        var result = await Uploader(handler).UploadAsync(
            filePath, Options(new ClientCredentials("pipeline", "s3cret")), TestContext.Current.CancellationToken);

        result.IsFailure.ShouldBeTrue();
        result.Failure.Code.ShouldBe("token_request_failed");
        result.Failure.Message.ShouldContain(expectedHint);
        result.Failure.Message.ShouldNotContain("s3cret");
        handler.Requests.ShouldHaveSingleItem();
    }

    [Fact]
    public async Task UploadAsync_IncludesTheProblemDetail_WhenTheGatewayAnswersWithProblemJson()
    {
        var handler = new StubHttpMessageHandler(_ => StubHttpMessageHandler.Json(
            HttpStatusCode.BadRequest, """{"detail":"'clientId' and 'clientSecret' are required."}""", "application/problem+json"));

        var result = await Uploader(handler).UploadAsync(
            filePath, Options(new ClientCredentials("pipeline", "s3cret")), TestContext.Current.CancellationToken);

        result.Failure.Message.ShouldContain("'clientId' and 'clientSecret' are required.");
    }

    [Theory]
    [InlineData("""{"tokenType":"Bearer"}""")]
    [InlineData("not json")]
    public async Task UploadAsync_Fails_WhenTheGatewayResponseHasNoAccessToken(string body)
    {
        var handler = new StubHttpMessageHandler(_ => StubHttpMessageHandler.Json(HttpStatusCode.OK, body));

        var result = await Uploader(handler).UploadAsync(
            filePath, Options(new ClientCredentials("pipeline", "s3cret")), TestContext.Current.CancellationToken);

        result.IsFailure.ShouldBeTrue();
        result.Failure.Code.ShouldBe("token_request_failed");
        handler.Requests.ShouldHaveSingleItem();
    }

    [Fact]
    public async Task UploadAsync_Fails_WhenTheGatewayIsUnreachable()
    {
        var handler = new StubHttpMessageHandler(_ => throw new HttpRequestException("connection refused"));

        var result = await Uploader(handler).UploadAsync(
            filePath, Options(new ClientCredentials("pipeline", "s3cret")), TestContext.Current.CancellationToken);

        result.IsFailure.ShouldBeTrue();
        result.Failure.Code.ShouldBe("token_request_failed");
        result.Failure.Message.ShouldContain("connection refused");
    }

    [Theory]
    [InlineData(HttpStatusCode.Unauthorized, "--token")]
    [InlineData(HttpStatusCode.NotFound, "--projectId")]
    [InlineData(HttpStatusCode.RequestEntityTooLarge, "maximum upload size")]
    [InlineData(HttpStatusCode.TooManyRequests, "retry later")]
    [InlineData(HttpStatusCode.InternalServerError, "HTTP 500")]
    public async Task UploadAsync_Fails_WithAHelpfulMessage_WhenTheIndexerRejectsTheUpload(HttpStatusCode status, string expectedHint)
    {
        var handler = new StubHttpMessageHandler(_ => new HttpResponseMessage(status));

        var result = await Uploader(handler).UploadAsync(filePath, Options(), TestContext.Current.CancellationToken);

        result.IsFailure.ShouldBeTrue();
        result.Failure.Code.ShouldBe("upload_rejected");
        result.Failure.Message.ShouldContain(expectedHint);
    }

    [Fact]
    public async Task UploadAsync_IncludesTheProblemDetail_WhenTheIndexerRejectsWithProblemJson()
    {
        var handler = new StubHttpMessageHandler(_ => StubHttpMessageHandler.Json(
            HttpStatusCode.BadRequest, """{"title":"Bad Request","detail":"The file extension must be .jsonl."}""", "application/problem+json"));

        var result = await Uploader(handler).UploadAsync(filePath, Options(), TestContext.Current.CancellationToken);

        result.Failure.Message.ShouldContain("The file extension must be .jsonl.");
    }

    [Fact]
    public async Task UploadAsync_Fails_WhenTheIndexerIsUnreachable()
    {
        var handler = new StubHttpMessageHandler(_ => throw new HttpRequestException("connection refused"));

        var result = await Uploader(handler).UploadAsync(filePath, Options(), TestContext.Current.CancellationToken);

        result.IsFailure.ShouldBeTrue();
        result.Failure.Code.ShouldBe("upload_unreachable");
        result.Failure.Message.ShouldContain("connection refused");
    }

    [Fact]
    public async Task UploadAsync_Fails_WhenTheResponseHasNoUploadId()
    {
        var handler = new StubHttpMessageHandler(_ => StubHttpMessageHandler.Json(HttpStatusCode.Accepted, """{"status":"pending"}"""));

        var result = await Uploader(handler).UploadAsync(filePath, Options(), TestContext.Current.CancellationToken);

        result.IsFailure.ShouldBeTrue();
        result.Failure.Code.ShouldBe("invalid_response");
    }

    [Fact]
    public async Task UploadAsync_Fails_WithoutCallingTheIndexer_WhenTheFileDoesNotExist()
    {
        var handler = new StubHttpMessageHandler(_ => StubHttpMessageHandler.Json(HttpStatusCode.Accepted, AcceptedJson));

        var result = await Uploader(handler).UploadAsync(filePath + ".missing", Options(), TestContext.Current.CancellationToken);

        result.IsFailure.ShouldBeTrue();
        result.Failure.Code.ShouldBe("file_not_found");
        handler.Requests.ShouldBeEmpty();
    }

    [Fact]
    public async Task UploadAsync_Propagates_WhenTheCallerCancels()
    {
        using var cancellation = new CancellationTokenSource();
        var handler = new StubHttpMessageHandler(_ =>
        {
            cancellation.Cancel();
            throw new TaskCanceledException();
        });

        await Should.ThrowAsync<TaskCanceledException>(() => Uploader(handler).UploadAsync(filePath, Options(), cancellation.Token));
    }

    private static HttpCiirUploader Uploader(StubHttpMessageHandler handler) => new(new HttpClient(handler));

    private static SendOptions Options(IndexerCredentials? credentials = null) => new()
    {
        BaseUrl = new Uri(BaseUrl),
        ProjectId = ProjectId,
        Credentials = credentials,
    };
}
