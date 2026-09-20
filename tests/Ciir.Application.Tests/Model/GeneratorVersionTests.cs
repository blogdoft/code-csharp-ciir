using Ciir.Application.Model;
using Shouldly;

namespace Ciir.Application.Tests.Model;

public class GeneratorVersionTests
{
    [Theory]
    [InlineData("1.2.3", "1.2.3")]
    [InlineData("1.2.3+8768c77cdaf8708835c183964426749cc9159fa9", "1.2.3")]
    [InlineData("0.2.0-beta.1", "0.2.0-beta.1")]
    [InlineData("0.2.0-beta.1+8768c77", "0.2.0-beta.1")]
    [InlineData("0.0.0-local", "0.0.0-local")]
    public void Normalize_RemovesBuildMetadata_AndKeepsPreReleaseLabel(string informationalVersion, string expected)
    {
        GeneratorVersion.Normalize(informationalVersion).ShouldBe(expected);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("+8768c77")]
    public void Normalize_FallsBackToZero_WhenThereIsNoUsableVersion(string? informationalVersion)
    {
        GeneratorVersion.Normalize(informationalVersion).ShouldBe("0.0.0");
    }

    [Fact]
    public void Current_HasNoBuildMetadata_AndIsNotEmpty()
    {
        GeneratorVersion.Current.ShouldNotBeNullOrWhiteSpace();
        GeneratorVersion.Current.ShouldNotContain('+');
    }
}
