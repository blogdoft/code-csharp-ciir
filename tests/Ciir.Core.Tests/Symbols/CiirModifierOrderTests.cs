using Ciir.Core.Symbols;
using Shouldly;

namespace Ciir.Core.Tests.Symbols;

public class CiirModifierOrderTests
{
    [Fact]
    public void Sort_OrdersModifiersCanonically_RegardlessOfInputOrder()
    {
        var shuffled = new[] { CiirModifier.Async, CiirModifier.Static, CiirModifier.Sealed };

        var sorted = CiirModifierOrder.Sort(shuffled);

        sorted.ShouldBe([CiirModifier.Static, CiirModifier.Sealed, CiirModifier.Async]);
    }

    [Fact]
    public void Sort_RemovesDuplicates()
    {
        var withDuplicates = new[] { CiirModifier.Async, CiirModifier.Async, CiirModifier.Static };

        var sorted = CiirModifierOrder.Sort(withDuplicates);

        sorted.ShouldBe([CiirModifier.Static, CiirModifier.Async]);
    }

    [Fact]
    public void Sort_ReturnsEmpty_WhenInputIsEmpty()
    {
        var sorted = CiirModifierOrder.Sort([]);

        sorted.ShouldBeEmpty();
    }

    [Fact]
    public void Canonical_ContainsEveryModifierExactlyOnce()
    {
        var allModifiers = Enum.GetValues<CiirModifier>();

        CiirModifierOrder.Canonical.Count.ShouldBe(allModifiers.Length);
        CiirModifierOrder.Canonical.Distinct().Count().ShouldBe(allModifiers.Length);
    }
}
