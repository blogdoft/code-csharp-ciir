namespace Ciir.Application.Model;

/// <summary>Process exit codes for the analysis use case, independent of the CLI's own argument-parsing errors.</summary>
public enum AnalysisExitCode
{
    /// <summary>The run completed successfully.</summary>
    Success = 0,

    /// <summary>The run completed with a fatal failure (e.g. a project failed under <c>--fail-on-error</c>).</summary>
    Failure = 1,

    /// <summary>The supplied input path/arguments were invalid.</summary>
    InvalidInput = 2,

    /// <summary>Writing the output artifacts failed.</summary>
    OutputWriteFailure = 3,
}
