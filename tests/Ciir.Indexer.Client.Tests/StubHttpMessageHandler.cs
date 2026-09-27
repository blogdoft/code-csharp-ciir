using System.Net;
using System.Text;

namespace Ciir.Indexer.Client.Tests;

/// <summary>An <see cref="HttpMessageHandler"/> that records every request (body included) and answers from a callback.</summary>
internal sealed class StubHttpMessageHandler(Func<RecordedRequest, HttpResponseMessage> respond) : HttpMessageHandler
{
    public List<RecordedRequest> Requests { get; } = [];

    public static HttpResponseMessage Json(HttpStatusCode status, string json, string mediaType = "application/json") =>
        new(status) { Content = new StringContent(json, Encoding.UTF8, mediaType) };

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var body = request.Content is null ? null : await request.Content.ReadAsStringAsync(cancellationToken);
        var recorded = new RecordedRequest(request.Method, request.RequestUri!, request.Headers.Authorization?.ToString(), body);
        Requests.Add(recorded);
        return respond(recorded);
    }
}
