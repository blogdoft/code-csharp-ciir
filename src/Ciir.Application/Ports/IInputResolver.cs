using BlogDoFT.Libs.ResultPattern;
using Ciir.Application.Model;

namespace Ciir.Application.Ports;

/// <summary>Classifies the raw <c>&lt;path&gt;</c> CLI argument as a solution, project, or directory.</summary>
public interface IInputResolver
{
    /// <summary>Resolves <paramref name="rawPath"/>, failing when it does not exist or has an unsupported extension.</summary>
    /// <param name="rawPath">The raw path argument supplied by the caller.</param>
    Result<AnalysisInput> Resolve(string rawPath);
}
