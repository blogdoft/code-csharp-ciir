using BlogDoFT.Libs.ResultPattern;
using Ciir.Application.Model;
using Ciir.Application.Ports;

namespace Ciir.Application.InputResolution;

/// <summary>Default <see cref="IInputResolver"/> based purely on filesystem inspection (no Roslyn involved).</summary>
public sealed class InputResolver : IInputResolver
{
    private static readonly string[] SolutionExtensions = [".sln", ".slnx"];

    /// <inheritdoc />
    public Result<AnalysisInput> Resolve(string rawPath)
    {
        ArgumentNullException.ThrowIfNull(rawPath);

        if (Directory.Exists(rawPath))
        {
            return Result<AnalysisInput>.FromSuccess(new AnalysisInput
            {
                Type = AnalysisInputType.Directory,
                Path = Path.GetFullPath(rawPath),
            });
        }

        if (!File.Exists(rawPath))
        {
            return Result<AnalysisInput>.FromFailure(new Failure("path_not_found", $"Path '{rawPath}' does not exist."));
        }

        var extension = Path.GetExtension(rawPath);

        if (SolutionExtensions.Contains(extension, StringComparer.OrdinalIgnoreCase))
        {
            return Result<AnalysisInput>.FromSuccess(new AnalysisInput
            {
                Type = AnalysisInputType.Solution,
                Path = Path.GetFullPath(rawPath),
            });
        }

        if (string.Equals(extension, ".csproj", StringComparison.OrdinalIgnoreCase))
        {
            return Result<AnalysisInput>.FromSuccess(new AnalysisInput
            {
                Type = AnalysisInputType.Project,
                Path = Path.GetFullPath(rawPath),
            });
        }

        return Result<AnalysisInput>.FromFailure(new Failure(
            "unsupported_file_type",
            $"'{rawPath}' is not a .sln, .slnx or .csproj file."));
    }
}
