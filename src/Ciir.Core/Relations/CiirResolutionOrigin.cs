namespace Ciir.Core.Relations;

/// <summary>Where a relation's target symbol originates from.</summary>
public enum CiirResolutionOrigin
{
    /// <summary>Origin could not be determined.</summary>
    Unknown,

    /// <summary>The target belongs to the analyzed project itself.</summary>
    Project,

    /// <summary>The target belongs to a different project analyzed in this same run (a same-solution project reference).</summary>
    Solution,

    /// <summary>The target belongs to a project/package dependency.</summary>
    Dependency,

    /// <summary>The target belongs to the language's standard framework/runtime library.</summary>
    Framework,

    /// <summary>The target is only known at runtime.</summary>
    Runtime,

    /// <summary>The target represents an external service boundary.</summary>
    ExternalService,
}
