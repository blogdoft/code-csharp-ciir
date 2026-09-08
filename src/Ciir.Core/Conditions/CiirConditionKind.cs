namespace Ciir.Core.Conditions;

/// <summary>The kind of conditional/branching construct a <see cref="CiirCondition"/> represents.</summary>
public enum CiirConditionKind
{
    /// <summary>An <c>if</c> statement.</summary>
    If,

    /// <summary>An <c>else if</c> statement.</summary>
    ElseIf,

    /// <summary>A <c>switch</c> statement.</summary>
    Switch,

    /// <summary>A <c>switch</c> expression.</summary>
    SwitchExpression,

    /// <summary>A <c>while</c> loop.</summary>
    While,

    /// <summary>A <c>do</c>/<c>while</c> loop.</summary>
    DoWhile,

    /// <summary>A <c>for</c> loop.</summary>
    For,

    /// <summary>A <c>foreach</c> loop.</summary>
    Foreach,

    /// <summary>A ternary conditional expression (<c>?:</c>).</summary>
    ConditionalExpression,

    /// <summary>An early-return/guard clause.</summary>
    Guard,
}
