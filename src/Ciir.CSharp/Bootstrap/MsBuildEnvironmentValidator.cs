using BlogDoFT.Libs.ResultPattern;
using Ciir.Application.Ports;

namespace Ciir.CSharp.Bootstrap;

/// <summary>
/// Validates that an MSBuild instance (from an installed .NET SDK) can be registered, which the
/// C# analyzer needs to open solutions and projects.
/// </summary>
public sealed class MsBuildEnvironmentValidator : IEnvironmentValidator
{
    /// <inheritdoc />
    public Result Validate()
    {
        try
        {
            MsBuildEnvironment.EnsureRegistered();
            return Result.AsSuccess();
        }
        catch (InvalidOperationException ex)
        {
            return Result.AsFailure(new Failure(
                "sdk_not_found",
                $"No .NET SDK was found, and ciir needs its MSBuild to open C# projects. Install the .NET 10 SDK (https://dot.net). Details: {ex.Message}"));
        }
    }
}
