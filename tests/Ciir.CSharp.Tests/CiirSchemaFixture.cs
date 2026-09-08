using Ciir.Serialization.SchemaProvider;
using Json.Schema;

namespace Ciir.CSharp.Tests;

/// <summary>
/// Parses <c>ciir.schema.json</c> exactly once for the whole test assembly. JsonSchema.Net
/// registers schemas globally by <c>$id</c>, so every test class parsing it independently would
/// throw <see cref="JsonSchemaException"/> on the second parse within the same process.
/// </summary>
internal static class CiirSchemaFixture
{
    public static readonly JsonSchema Schema = JsonSchema.FromText(CiirSchemaProvider.GetSchemaJson());
}
