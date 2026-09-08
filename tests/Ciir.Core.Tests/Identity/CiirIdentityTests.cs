using Ciir.Core.Identity;
using Shouldly;

namespace Ciir.Core.Tests.Identity;

public class CiirIdentityTests
{
    [Fact]
    public void ComputeId_IsDeterministic_AcrossRepeatedCalls()
    {
        var first = CiirIdentity.ComputeId("csharp", "Payments.Application", CiirKind.Method, "Payments.Application.PaymentService.AuthorizeAsync(Payments.Domain.Order)");
        var second = CiirIdentity.ComputeId("csharp", "Payments.Application", CiirKind.Method, "Payments.Application.PaymentService.AuthorizeAsync(Payments.Domain.Order)");

        first.ShouldBe(second);
    }

    [Fact]
    public void ComputeId_StartsWithSha256Prefix()
    {
        var id = CiirIdentity.ComputeId("csharp", "Payments.Application", CiirKind.Type, "Payments.Application.PaymentService");

        id.ShouldStartWith("sha256:");
    }

    [Fact]
    public void ComputeId_DiffersForDifferentOverloads()
    {
        var withOneParameter = CiirIdentity.ComputeId(
            "csharp",
            "Payments.Application",
            CiirKind.Method,
            "Payments.Application.PaymentService.AuthorizeAsync(Payments.Domain.Order)");
        var withTwoParameters = CiirIdentity.ComputeId(
            "csharp",
            "Payments.Application",
            CiirKind.Method,
            "Payments.Application.PaymentService.AuthorizeAsync(Payments.Domain.Order,System.Threading.CancellationToken)");

        withOneParameter.ShouldNotBe(withTwoParameters);
    }

    [Fact]
    public void ComputeId_DiffersWhenOnlyKindDiffers()
    {
        var asType = CiirIdentity.ComputeId("csharp", "Payments.Domain", CiirKind.Type, "Payments.Domain.Order");
        var asNamespace = CiirIdentity.ComputeId("csharp", "Payments.Domain", CiirKind.Namespace, "Payments.Domain.Order");

        asType.ShouldNotBe(asNamespace);
    }

    [Fact]
    public void ComputeId_DiffersWhenOnlyProjectDiffers()
    {
        var inFirstProject = CiirIdentity.ComputeId("csharp", "Payments.Application", CiirKind.Type, "Payments.Domain.Order");
        var inSecondProject = CiirIdentity.ComputeId("csharp", "Payments.Domain", CiirKind.Type, "Payments.Domain.Order");

        inFirstProject.ShouldNotBe(inSecondProject);
    }

    [Fact]
    public void ComputeId_DiffersWhenOnlyLanguageDiffers()
    {
        var csharpId = CiirIdentity.ComputeId("csharp", "Payments.Domain", CiirKind.Type, "Payments.Domain.Order");
        var javaId = CiirIdentity.ComputeId("java", "Payments.Domain", CiirKind.Type, "Payments.Domain.Order");

        csharpId.ShouldNotBe(javaId);
    }

    [Fact]
    public void BuildCanonicalKey_JoinsComponentsWithPipeSeparator()
    {
        var key = CiirIdentity.BuildCanonicalKey("csharp", "Payments.Application", CiirKind.Method, "Payments.Application.PaymentService.AuthorizeAsync()");

        key.ShouldBe("csharp|Payments.Application|method|Payments.Application.PaymentService.AuthorizeAsync()");
    }
}
