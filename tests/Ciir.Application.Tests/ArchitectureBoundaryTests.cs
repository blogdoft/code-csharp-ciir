using Ciir.Core;
using Shouldly;
using System.Reflection;

namespace Ciir.Application.Tests;

/// <summary>
/// Enforces the hexagonal architecture's central rule (spec section 74) automatically: neither
/// <c>Ciir.Core</c> nor <c>Ciir.Application</c> may reference Roslyn. Convention alone is easy to
/// violate by accident with a stray <c>using Microsoft.CodeAnalysis;</c>.
/// </summary>
public class ArchitectureBoundaryTests
{
    [Fact]
    public void CiirCore_DoesNotReferenceRoslyn()
    {
        AssertNoRoslynReference(typeof(CiirDocument).Assembly);
    }

    [Fact]
    public void CiirApplication_DoesNotReferenceRoslyn()
    {
        AssertNoRoslynReference(typeof(Ports.ICodeAnalyzer).Assembly);
    }

    [Fact]
    public void CiirConfiguration_DoesNotReferenceRoslyn()
    {
        AssertNoRoslynReference(typeof(Configuration.ConfigurationCodeAnalyzer).Assembly);
    }

    private static void AssertNoRoslynReference(Assembly assembly)
    {
        var referencedRoslynAssemblies = assembly.GetReferencedAssemblies()
            .Where(reference => reference.Name!.StartsWith("Microsoft.CodeAnalysis", StringComparison.Ordinal))
            .Select(reference => reference.Name)
            .ToArray();

        referencedRoslynAssemblies.ShouldBeEmpty(
            $"{assembly.GetName().Name} must not reference Roslyn, but references: {string.Join(", ", referencedRoslynAssemblies)}");
    }
}
