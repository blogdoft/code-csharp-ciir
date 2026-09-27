using Ciir.Application.Model;
using Ciir.Cli.Composition;
using Shouldly;

namespace Ciir.Cli.Tests;

public class SendOptionsFactoryTests
{
    private static readonly string ProjectId = "3f2b1c0e-5a4d-4e8b-9c1a-0d2e3f4a5b6c";

    [Fact]
    public void Create_UsesTheArgument_OverTheEnvironmentVariable()
    {
        var result = SendOptionsFactory.Create("https://from-arg.example", "https://from-env.example", ProjectId, null, null, null);

        result.IsSuccess.ShouldBeTrue();
        result.Value.BaseUrl.ToString().ShouldBe("https://from-arg.example/");
        result.Value.ProjectId.ShouldBe(Guid.Parse(ProjectId));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("  ")]
    public void Create_FallsBackToTheEnvironmentVariable_WhenTheArgumentIsEmpty(string? argument)
    {
        var result = SendOptionsFactory.Create(argument, "https://from-env.example", ProjectId, null, null, null);

        result.IsSuccess.ShouldBeTrue();
        result.Value.BaseUrl.ToString().ShouldBe("https://from-env.example/");
    }

    [Fact]
    public void Create_Fails_MentioningTheEnvironmentVariable_WhenThereIsNoBaseUrlAnywhere()
    {
        var result = SendOptionsFactory.Create(null, null, ProjectId, null, null, null);

        result.IsFailure.ShouldBeTrue();
        result.Failure.Code.ShouldBe("base_url_missing");
        result.Failure.Message.ShouldContain("CIIR_BASE_URL");
        result.Failure.Message.ShouldContain("<base-url>");
    }

    [Theory]
    [InlineData("not a url")]
    [InlineData("/relative/path")]
    [InlineData("ftp://indexer.example")]
    public void Create_Fails_WithTheOffendingValue_WhenTheBaseUrlIsInvalid(string baseUrl)
    {
        var result = SendOptionsFactory.Create(baseUrl, null, ProjectId, null, null, null);

        result.IsFailure.ShouldBeTrue();
        result.Failure.Code.ShouldBe("base_url_invalid");
        result.Failure.Message.ShouldContain(baseUrl);
    }

    [Theory]
    [InlineData("https://indexer.example/code-brain", "https://indexer.example/code-brain/")]
    [InlineData("https://indexer.example/code-brain/", "https://indexer.example/code-brain/")]
    [InlineData("http://localhost:5223", "http://localhost:5223/")]
    [InlineData("https://indexer.example/code-brain?x=1#y", "https://indexer.example/code-brain/")]
    public void Create_NormalizesTheBaseUrl_ToEndWithASlash(string baseUrl, string expected)
    {
        var result = SendOptionsFactory.Create(baseUrl, null, ProjectId, null, null, null);

        result.Value.BaseUrl.ToString().ShouldBe(expected);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("not-a-guid")]
    [InlineData("00000000-0000-0000-0000-000000000000")]
    public void Create_Fails_WhenTheProjectIdIsMissingOrInvalid(string? projectId)
    {
        var result = SendOptionsFactory.Create("https://indexer.example", null, projectId, null, null, null);

        result.IsFailure.ShouldBeTrue();
        result.Failure.Code.ShouldBe("project_id_invalid");
        result.Failure.Message.ShouldContain("--projectId");
    }

    [Fact]
    public void Create_SendsWithoutAuthentication_WhenNoCredentialsAreGiven()
    {
        var result = SendOptionsFactory.Create("https://indexer.example", null, ProjectId, null, null, null);

        result.Value.Credentials.ShouldBeNull();
    }

    [Fact]
    public void Create_UsesTheToken_OverClientCredentials()
    {
        var result = SendOptionsFactory.Create("https://indexer.example", null, ProjectId, "abc.def", "id", "secret");

        result.Value.Credentials.ShouldBe(new BearerTokenCredentials("abc.def"));
    }

    [Fact]
    public void Create_UsesClientCredentials_WhenBothAreGiven()
    {
        var result = SendOptionsFactory.Create("https://indexer.example", null, ProjectId, null, "id", "secret");

        result.Value.Credentials.ShouldBe(new ClientCredentials("id", "secret"));
    }

    [Theory]
    [InlineData("id", null)]
    [InlineData(null, "secret")]
    public void Create_IgnoresClientCredentials_WhenOnlyOneIsGiven(string? clientId, string? clientSecret)
    {
        var result = SendOptionsFactory.Create("https://indexer.example", null, ProjectId, null, clientId, clientSecret);

        result.Value.Credentials.ShouldBeNull();
    }

    [Theory]
    [InlineData(null, "id", null, true)]
    [InlineData(null, null, "secret", true)]
    [InlineData(null, "id", "secret", false)]
    [InlineData(null, null, null, false)]
    [InlineData("abc", "id", null, false)]
    public void HasIncompleteClientCredentials_WarnsOnlyWhenExactlyOneIsGivenWithoutAToken(string? token, string? clientId, string? clientSecret, bool expected)
    {
        SendOptionsFactory.HasIncompleteClientCredentials(token, clientId, clientSecret).ShouldBe(expected);
    }
}
