using Shouldly;

namespace Ciir.Cli.Tests;

/// <summary>
/// Runs the built <c>ciir</c> CLI as a real subprocess and asserts on its exit codes, matching
/// how a script or CI pipeline would actually invoke it.
/// </summary>
public class CliExitCodeTests
{
    [Fact]
    public async Task Run_ReturnsInvalidInput_ForNonexistentPath()
    {
        var outputDirectory = CreateTempOutputDirectory();

        var result = await CliRunner.RunAsync(["/definitely/does/not/exist.sln", "--output", outputDirectory]);

        result.ExitCode.ShouldBe(2);
        result.StandardError.ShouldContain("does not exist");
    }

    [Fact]
    public async Task Run_ReturnsInvalidInput_WhenPathArgumentIsMissing()
    {
        var result = await CliRunner.RunAsync([]);

        result.ExitCode.ShouldBe(2);
    }

    [Fact]
    public async Task Run_ReturnsInvalidInput_ForUnsupportedFileExtension()
    {
        var outputDirectory = CreateTempOutputDirectory();
        var readmePath = Path.Combine(Path.GetTempPath(), $"ciir-cli-tests-{Guid.NewGuid()}.md");
        await File.WriteAllTextAsync(readmePath, string.Empty, TestContext.Current.CancellationToken);

        try
        {
            var result = await CliRunner.RunAsync([readmePath, "--output", outputDirectory]);

            result.ExitCode.ShouldBe(2);
        }
        finally
        {
            File.Delete(readmePath);
        }
    }

    [Fact]
    public async Task Run_ReturnsInvalidInput_BeforeAnalyzing_WhenSendHasNoBaseUrlAndNoEnvironmentVariable()
    {
        var outputDirectory = CreateTempOutputDirectory();

        var result = await CliRunner.RunAsync([Path.GetTempPath(), "--output", outputDirectory, "--send", "--projectId", Guid.NewGuid().ToString()]);

        result.ExitCode.ShouldBe(2);
        result.StandardError.ShouldContain("CIIR_BASE_URL");
        Directory.Exists(outputDirectory).ShouldBeFalse("validation must fail before the analysis starts");
    }

    [Fact]
    public async Task Run_ReturnsInvalidInput_WhenTheBaseUrlComesFromAnInvalidEnvironmentVariable()
    {
        var result = await CliRunner.RunAsync(
            [Path.GetTempPath(), "-s", "--projectId", Guid.NewGuid().ToString()],
            new Dictionary<string, string> { ["CIIR_BASE_URL"] = "not a url" });

        result.ExitCode.ShouldBe(2);
        result.StandardError.ShouldContain("not a url");
    }

    [Fact]
    public async Task Run_ReturnsInvalidInput_WhenSendHasNoProjectId()
    {
        var result = await CliRunner.RunAsync([Path.GetTempPath(), "--send", "https://indexer.example"]);

        result.ExitCode.ShouldBe(2);
        result.StandardError.ShouldContain("--projectId");
    }

    private static string CreateTempOutputDirectory() =>
        Path.Combine(Path.GetTempPath(), "ciir-cli-tests-" + Guid.NewGuid());
}
