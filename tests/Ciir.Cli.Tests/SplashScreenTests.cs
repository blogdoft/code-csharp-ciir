using Ciir.Cli.Presentation;
using Shouldly;

namespace Ciir.Cli.Tests;

public class SplashScreenTests
{
    private const string Version = "1.2.3-beta.1";

    [Fact]
    public void Render_ContainsToolVersion_BlogDoFtArtAndBlogLink()
    {
        var text = SplashScreen.Render(Version);

        text.ShouldContain($"Code Intelligence IR - v{Version}");
        text.ShouldContain(@"|____/ |_| \___/  \__, | |____/  \___/ |_|      |_|");
        text.ShouldContain("https://www.blogdoft.com.br/");
    }

    [Fact]
    public void Render_ShowsBanner_ThenBlogLink_ThenNotices()
    {
        var text = SplashScreen.Render(Version);

        var toolNameIndex = text.IndexOf(@"\____||___||___||_| \_\", StringComparison.Ordinal);
        var blogDoFtIndex = text.IndexOf(@"|____/ |_| \___/", StringComparison.Ordinal);
        var linkIndex = text.IndexOf(SplashScreen.BlogUrl, StringComparison.Ordinal);
        var sdkNoticeIndex = text.IndexOf(".NET 10 SDK", StringComparison.Ordinal);
        var restoreNoticeIndex = text.IndexOf("dotnet restore", StringComparison.Ordinal);
        var globalJsonNoticeIndex = text.IndexOf("global.json", StringComparison.Ordinal);

        toolNameIndex.ShouldBeGreaterThanOrEqualTo(0);
        blogDoFtIndex.ShouldBeGreaterThan(toolNameIndex);
        linkIndex.ShouldBeGreaterThan(blogDoFtIndex);
        sdkNoticeIndex.ShouldBeGreaterThan(linkIndex);
        restoreNoticeIndex.ShouldBeGreaterThan(sdkNoticeIndex);
        globalJsonNoticeIndex.ShouldBeGreaterThan(restoreNoticeIndex);
    }

    [Fact]
    public void Render_UsesOnlyAscii_AndNoAnsiEscapes()
    {
        var text = SplashScreen.Render(Version);

        text.ShouldAllBe(character => character < 128);
        text.ShouldNotContain('\u001b');
    }

    [Fact]
    public void Render_EndsWithBlankLine_SeparatingItFromToolOutput()
    {
        var text = SplashScreen.Render(Version);

        text.ShouldEndWith(Environment.NewLine + Environment.NewLine);
    }

    [Fact]
    public void Render_FitsInEightyColumns()
    {
        var lines = SplashScreen.Render(Version).Split(Environment.NewLine);

        lines.ShouldAllBe(line => line.Length <= 80);
    }

    [Theory]
    [InlineData(false, null, false)]
    [InlineData(false, "", false)]
    [InlineData(false, "0", false)]
    [InlineData(false, "no", false)]
    [InlineData(true, null, true)]
    [InlineData(false, "1", true)]
    [InlineData(false, "true", true)]
    [InlineData(false, "TRUE", true)]
    [InlineData(false, " True ", true)]
    [InlineData(true, "0", true)]
    public void IsSuppressed_HonorsOptionAndEnvironmentVariable(bool noBannerOption, string? environmentValue, bool expected)
    {
        SplashScreen.IsSuppressed(noBannerOption, environmentValue).ShouldBe(expected);
    }

    [Fact]
    public void Write_WritesRenderedTextToWriter()
    {
        using var writer = new StringWriter();

        SplashScreen.Write(writer, Version);

        writer.ToString().ShouldBe(SplashScreen.Render(Version));
    }
}
