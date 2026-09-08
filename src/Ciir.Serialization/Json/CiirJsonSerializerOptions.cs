using Ciir.Serialization.Json.Converters;
using System.Collections;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;

namespace Ciir.Serialization.Json;

/// <summary>
/// Builds the shared <see cref="JsonSerializerOptions"/> used to read and write CIIR documents:
/// camelCase property names, snake_case-lower enum tokens (matching <c>ciir.schema.json</c>), and
/// omission of empty collections so the JSONL output never carries large empty structures.
/// </summary>
public static class CiirJsonSerializerOptions
{
    /// <summary>Creates a new, independently configured options instance.</summary>
    public static JsonSerializerOptions Create()
    {
        var options = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            TypeInfoResolver = new DefaultJsonTypeInfoResolver().WithAddedModifier(OmitEmptyCollections),
            WriteIndented = false,
        };

        options.Converters.Add(new CiirDocumentationFormatJsonConverter());
        options.Converters.Add(new JsonStringEnumConverter(JsonNamingPolicy.SnakeCaseLower));

        return options;
    }

    private static void OmitEmptyCollections(JsonTypeInfo typeInfo)
    {
        foreach (var property in typeInfo.Properties)
        {
            if (!typeof(IEnumerable).IsAssignableFrom(property.PropertyType) || property.PropertyType == typeof(string))
            {
                continue;
            }

            var existingPredicate = property.ShouldSerialize;
            property.ShouldSerialize = (instance, value) =>
                !IsEmptyCollection(value) && (existingPredicate is null || existingPredicate(instance, value));
        }
    }

    private static bool IsEmptyCollection(object? value) => value is ICollection { Count: 0 };
}
