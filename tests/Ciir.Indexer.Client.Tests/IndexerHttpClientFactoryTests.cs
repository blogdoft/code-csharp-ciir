using Shouldly;
using System.Net;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;

namespace Ciir.Indexer.Client.Tests;

public sealed class IndexerHttpClientFactoryTests : IDisposable
{
    private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(30);

    private readonly X509Certificate2 certificate = CreateSelfSignedCertificate();
    private readonly TcpListener listener = new(IPAddress.Loopback, 0);

    public IndexerHttpClientFactoryTests()
    {
        listener.Start();
        _ = ServeAsync();
    }

    private Uri Url => new($"https://localhost:{((IPEndPoint)listener.LocalEndpoint).Port}/");

    public void Dispose()
    {
        listener.Stop();
        certificate.Dispose();
    }

    [Fact]
    public async Task Create_RejectsAnUntrustedCertificate_ByDefault()
    {
        using var client = IndexerHttpClientFactory.Create(Timeout, insecure: false);

        var exception = await Should.ThrowAsync<HttpRequestException>(() => client.GetAsync(Url, TestContext.Current.CancellationToken));

        exception.InnerException.ShouldBeOfType<System.Security.Authentication.AuthenticationException>();
    }

    [Fact]
    public async Task Create_AcceptsAnUntrustedCertificate_WhenInsecure()
    {
        using var client = IndexerHttpClientFactory.Create(Timeout, insecure: true);

        using var response = await client.GetAsync(Url, TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Fact]
    public void Create_AppliesTheTimeout()
    {
        using var client = IndexerHttpClientFactory.Create(Timeout, insecure: false);

        client.Timeout.ShouldBe(Timeout);
    }

    private static X509Certificate2 CreateSelfSignedCertificate()
    {
        using var key = RSA.Create(2048);
        var request = new CertificateRequest("CN=localhost", key, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
        request.CertificateExtensions.Add(new X509EnhancedKeyUsageExtension([new Oid("1.3.6.1.5.5.7.3.1")], critical: false));
        var sanBuilder = new SubjectAlternativeNameBuilder();
        sanBuilder.AddDnsName("localhost");
        request.CertificateExtensions.Add(sanBuilder.Build());

        using var ephemeral = request.CreateSelfSigned(DateTimeOffset.UtcNow.AddDays(-1), DateTimeOffset.UtcNow.AddDays(1));

        // Round-trips through PFX so the private key is usable by SslStream on every platform.
        return X509CertificateLoader.LoadPkcs12(ephemeral.Export(X509ContentType.Pfx), password: null);
    }

    private async Task ServeAsync()
    {
        try
        {
            while (true)
            {
                var tcpClient = await listener.AcceptTcpClientAsync();
                _ = HandleAsync(tcpClient);
            }
        }
        catch (Exception ex) when (ex is ObjectDisposedException or SocketException or InvalidOperationException)
        {
            // The listener was stopped: the test is over.
        }
    }

    private async Task HandleAsync(TcpClient tcpClient)
    {
        try
        {
            using (tcpClient)
            await using (var ssl = new SslStream(tcpClient.GetStream()))
            {
                await ssl.AuthenticateAsServerAsync(certificate);
                _ = await ssl.ReadAsync(new byte[4096]);
                await ssl.WriteAsync(Encoding.ASCII.GetBytes("HTTP/1.1 200 OK\r\nContent-Length: 0\r\nConnection: close\r\n\r\n"));
                await ssl.FlushAsync();
            }
        }
        catch (Exception ex) when (ex is IOException or System.Security.Authentication.AuthenticationException or ObjectDisposedException)
        {
            // The client walked away (e.g. it rejected the certificate): expected in the default-rejects test.
        }
    }
}
