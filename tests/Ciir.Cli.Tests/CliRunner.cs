using System.Diagnostics;

namespace Ciir.Cli.Tests;

/// <summary>Runs the built <c>ciir</c> CLI as a real subprocess, the way a script or CI pipeline would.</summary>
internal static class CliRunner
{
    public static async Task<(int ExitCode, string StandardOutput, string StandardError)> RunAsync(
        IReadOnlyList<string> arguments,
        IReadOnlyDictionary<string, string>? environment = null)
    {
        var cliPath = Path.Combine(AppContext.BaseDirectory, "ciir.dll");
        var startInfo = new ProcessStartInfo("dotnet", [cliPath, .. arguments])
        {
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
        };

        // A developer/CI shell may already export CIIR_NOLOGO; tests must control it explicitly.
        startInfo.Environment.Remove("CIIR_NOLOGO");
        foreach (var (name, value) in environment ?? new Dictionary<string, string>())
        {
            startInfo.Environment[name] = value;
        }

        using var process = Process.Start(startInfo) ?? throw new InvalidOperationException("Failed to start the CLI process.");
        var standardOutputTask = process.StandardOutput.ReadToEndAsync();
        var standardErrorTask = process.StandardError.ReadToEndAsync();

        await process.WaitForExitAsync(TestContext.Current.CancellationToken);

        return (process.ExitCode, await standardOutputTask, await standardErrorTask);
    }
}
