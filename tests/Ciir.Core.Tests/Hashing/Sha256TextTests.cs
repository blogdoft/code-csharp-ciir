using Ciir.Core.Hashing;
using Shouldly;

namespace Ciir.Core.Tests.Hashing;

public class Sha256TextTests
{
    [Fact]
    public void ComputeHash_IsDeterministic()
    {
        var first = Sha256Text.ComputeHash("hello world");
        var second = Sha256Text.ComputeHash("hello world");

        first.ShouldBe(second);
    }

    [Fact]
    public void ComputeHash_DiffersForDifferentInput()
    {
        var first = Sha256Text.ComputeHash("hello world");
        var second = Sha256Text.ComputeHash("hello world!");

        first.ShouldNotBe(second);
    }

    [Fact]
    public void ComputeHash_ProducesLowercaseHex()
    {
        var hash = Sha256Text.ComputeHash("hello world");

        hash.ShouldMatch("^[0-9a-f]{64}$");
    }

    [Fact]
    public void ComputePrefixedHash_AddsSha256Prefix()
    {
        var prefixed = Sha256Text.ComputePrefixedHash("hello world");

        prefixed.ShouldStartWith("sha256:");
        prefixed["sha256:".Length..].ShouldBe(Sha256Text.ComputeHash("hello world"));
    }
}
