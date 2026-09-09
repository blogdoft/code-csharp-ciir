using Ciir.CSharp.SourceEvidence;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Shouldly;

namespace Ciir.CSharp.Tests.SourceEvidence;

public class SourceLocationFactoryTests
{
    [Fact]
    public void Create_ComputesPath_RelativeToRootDirectory_NotTheFilesOwnFolder()
    {
        var rootDirectory = Path.Combine(Path.GetTempPath(), "ciir-root");
        var filePath = Path.Combine(rootDirectory, "src", "Api", "Dto", "Fixture.cs");
        var declaration = ParseClassDeclaration(filePath);

        var location = SourceLocationFactory.Create(declaration, rootDirectory, includeSource: false);

        location.Path.ShouldBe("src/Api/Dto/Fixture.cs");
    }

    [Fact]
    public void Create_NormalizesDirectorySeparators_ToForwardSlash()
    {
        var rootDirectory = Path.Combine(Path.GetTempPath(), "ciir-root");
        var filePath = Path.Combine(rootDirectory, "nested", "folder", "Fixture.cs");
        var declaration = ParseClassDeclaration(filePath);

        var location = SourceLocationFactory.Create(declaration, rootDirectory, includeSource: false);

        location.Path.ShouldNotContain("\\");
        location.Path.ShouldBe("nested/folder/Fixture.cs");
    }

    private static ClassDeclarationSyntax ParseClassDeclaration(string filePath)
    {
        var tree = CSharpSyntaxTree.ParseText("class Fixture { }", path: filePath);
        return tree.GetRoot().DescendantNodes().OfType<ClassDeclarationSyntax>().Single();
    }
}
