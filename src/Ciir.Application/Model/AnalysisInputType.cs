namespace Ciir.Application.Model;

/// <summary>What kind of filesystem path the <c>&lt;path&gt;</c> CLI argument resolved to.</summary>
public enum AnalysisInputType
{
    /// <summary>A <c>.sln</c> or <c>.slnx</c> solution file.</summary>
    Solution,

    /// <summary>A <c>.csproj</c> project file.</summary>
    Project,

    /// <summary>A directory to be recursively scanned for projects.</summary>
    Directory,
}
