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

    private static string CreateTempOutputDirectory() =>
        Path.Combine(Path.GetTempPath(), "ciir-cli-tests-" + Guid.NewGuid());
}
