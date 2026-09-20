using BlogDoFT.Libs.ResultPattern;

namespace Ciir.Application.Ports;

/// <summary>
/// Verifies that the machine can run an analysis at all (e.g. that the toolchain the analyzer
/// relies on is installed), so the run fails fast with an actionable error instead of reporting
/// every project as failed.
/// </summary>
public interface IEnvironmentValidator
{
    /// <summary>Validates the environment, failing with an actionable message when it cannot run an analysis.</summary>
    Result Validate();
}
