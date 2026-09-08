using Microsoft.Build.Locator;

namespace Ciir.CSharp.Bootstrap;

/// <summary>
/// Registers the installed .NET SDK's MSBuild assemblies so <c>MSBuildWorkspace</c> can be
/// created. Must run before the first <c>MSBuildWorkspace.Create()</c> call in the process.
/// </summary>
public static class MsBuildEnvironment
{
    private static readonly Lock RegistrationLock = new();
    private static bool registered;

    /// <summary>Registers the default MSBuild instance, if not already registered.</summary>
    public static void EnsureRegistered()
    {
        if (registered)
        {
            return;
        }

        lock (RegistrationLock)
        {
            if (registered)
            {
                return;
            }

            if (!MSBuildLocator.IsRegistered)
            {
                MSBuildLocator.RegisterDefaults();
            }

            registered = true;
        }
    }
}
