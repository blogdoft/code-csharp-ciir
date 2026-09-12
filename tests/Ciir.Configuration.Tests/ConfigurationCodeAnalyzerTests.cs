using Shouldly;

namespace Ciir.Configuration.Tests;

public class ConfigurationCodeAnalyzerTests
{
    private readonly ConfigurationCodeAnalyzer analyzer = new();

    [Theory]
    [InlineData("appsettings.json", true)]
    [InlineData("appsettings.Production.json", true)]
    [InlineData("Appsettings.JSON", true)]
    [InlineData("APPSETTINGS.json", true)]
    [InlineData("config.yaml", true)]
    [InlineData("config.yml", true)]
    [InlineData("Config.YAML", true)]
    [InlineData("settings.json", false)]
    [InlineData("Program.cs", false)]
    [InlineData("Project.csproj", false)]
    [InlineData("readme.md", false)]
    public void CanAnalyze_MatchesOnlyAppSettingsJsonAndYaml(string fileName, bool expected)
    {
        analyzer.CanAnalyze(Path.Combine("some", "dir", fileName)).ShouldBe(expected);
    }
}
