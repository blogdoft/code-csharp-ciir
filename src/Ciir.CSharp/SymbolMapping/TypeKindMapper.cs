using Ciir.Core.Symbols;
using Microsoft.CodeAnalysis;

namespace Ciir.CSharp.SymbolMapping;

/// <summary>Maps an <see cref="INamedTypeSymbol"/> to <see cref="CiirTypeKind"/>.</summary>
internal static class TypeKindMapper
{
    public static CiirTypeKind Map(INamedTypeSymbol symbol)
    {
        ArgumentNullException.ThrowIfNull(symbol);

        if (symbol.IsRecord)
        {
            return CiirTypeKind.Record;
        }

        return symbol.TypeKind switch
        {
            TypeKind.Class => CiirTypeKind.Class,
            TypeKind.Interface => CiirTypeKind.Interface,
            TypeKind.Struct => CiirTypeKind.Struct,
            TypeKind.Enum => CiirTypeKind.Enum,
            TypeKind.Delegate => CiirTypeKind.Delegate,
            _ => CiirTypeKind.Unknown,
        };
    }
}
