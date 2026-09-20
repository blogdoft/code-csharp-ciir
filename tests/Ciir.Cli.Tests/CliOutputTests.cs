using Ciir.Cli.Presentation;
using Shouldly;

namespace Ciir.Cli.Tests;

/// <summary>
/// Runs the built <c>ciir</c> CLI as a real subprocess and asserts on what it prints: the
/// splash screen appears for analysis runs (before anything else), and never for
/// <c>--help</c>/<c>--version</c>, so their output stays script-friendly.
/// </summary>
public class CliOutputTests
{
    private static readonly string MissingPath = "/definitely/does/not/exist.sln";

    [Fact]
    public async Task Version_PrintsVersionOnly_WithoutSplash()
    {
        var result = await CliRunner.RunAsync(["--version"]);

        result.ExitCode.ShouldBe(0);
        result.StandardOutput.Trim().ShouldMatch(@"^\d+\.\d+\.\d+(-[0-9A-Za-z.-]+)?(\+[0-9A-Za-z.-]+)?$");
        result.StandardOutput.ShouldNotContain(SplashScreen.BlogUrl);
    }

    [Fact]
    public async Task Help_ListsNoBannerOption_WithoutSplash()
    {
        var result = await CliRunner.RunAsync(["--help"]);

        result.ExitCode.ShouldBe(0);
        result.StandardOutput.ShouldContain("--no-banner");
        result.StandardOutput.ShouldNotContain(SplashScreen.BlogUrl);
    }

    [Fact]
    public async Task Analysis_ShowsSplash_OnStdout_AndErrorOnStderr_WhenPathDoesNotExist()
    {
        var result = await CliRunner.RunAsync([MissingPath]);

        result.ExitCode.ShouldBe(2);
        result.StandardOutput.ShouldStartWith("  ____  ___  ___  ____");
        result.StandardOutput.ShouldContain(SplashScreen.BlogUrl);
        result.StandardError.ShouldContain("does not exist");
        result.StandardError.ShouldNotContain(SplashScreen.BlogUrl);
    }

    [Fact]
    public async Task Analysis_ShowsNoticesAfterLink_AndBeforeToolOutput()
    {
        var outputDirectory = Path.Combine(Path.GetTempPath(), "ciir-cli-tests-" + Guid.NewGuid());
        var projectDirectory = Path.Combine(Path.GetTempPath(), "ciir-cli-tests-" + Guid.NewGuid());
        Directory.CreateDirectory(projectDirectory);

        try
        {
            var result = await CliRunner.RunAsync([projectDirectory, "--output", outputDirectory]);

            result.ExitCode.ShouldBe(0);
            var linkIndex = result.StandardOutput.IndexOf(SplashScreen.BlogUrl, StringComparison.Ordinal);
            var noticeIndex = result.StandardOutput.IndexOf("global.json", StringComparison.Ordinal);
            var toolOutputIndex = result.StandardOutput.IndexOf("Discovering projects...", StringComparison.Ordinal);

            linkIndex.ShouldBeGreaterThanOrEqualTo(0);
            noticeIndex.ShouldBeGreaterThan(linkIndex);
            toolOutputIndex.ShouldBeGreaterThan(noticeIndex);
        }
        finally
        {
            Directory.Delete(projectDirectory, recursive: true);
            if (Directory.Exists(outputDirectory))
            {
                Directory.Delete(outputDirectory, recursive: true);
            }
        }
    }

    [Fact]
    public async Task Analysis_SuppressesSplash_WithNoBannerOption()
    {
        var result = await CliRunner.RunAsync([MissingPath, "--no-banner"]);

        result.ExitCode.ShouldBe(2);
        result.StandardOutput.ShouldNotContain(SplashScreen.BlogUrl);
        result.StandardOutput.ShouldNotContain("Before you start");
    }

    [Theory]
    [InlineData("1")]
    [InlineData("true")]
    public async Task Analysis_SuppressesSplash_WithNoLogoEnvironmentVariable(string value)
    {
        var result = await CliRunner.RunAsync(
            [MissingPath],
            new Dictionary<string, string> { [SplashScreen.NoLogoEnvironmentVariable] = value });

        result.ExitCode.ShouldBe(2);
        result.StandardOutput.ShouldNotContain(SplashScreen.BlogUrl);
        result.StandardOutput.ShouldNotContain("Before you start");
    }
}
