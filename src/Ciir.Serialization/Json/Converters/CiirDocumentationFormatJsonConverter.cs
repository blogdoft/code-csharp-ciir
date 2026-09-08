using Ciir.Core.Documentation;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Ciir.Serialization.Json.Converters;

/// <summary>
/// Converts <see cref="CiirDocumentationFormat"/> using the exact tokens declared in
/// <c>ciir.schema.json</c>. A dedicated converter is needed only because <see cref="CiirDocumentationFormat.XmlDoc"/>
/// serializes as the hyphenated <c>"xml-doc"</c>; every other value matches the default
/// snake_case-lower convention used for all other CIIR enums.
/// </summary>
internal sealed class CiirDocumentationFormatJsonConverter : JsonConverter<CiirDocumentationFormat>
{
    public override CiirDocumentationFormat Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) =>
        reader.GetString() switch
        {
            "xml-doc" => CiirDocumentationFormat.XmlDoc,
            "javadoc" => CiirDocumentationFormat.Javadoc,
            "jsdoc" => CiirDocumentationFormat.Jsdoc,
            "tsdoc" => CiirDocumentationFormat.Tsdoc,
            "docstring" => CiirDocumentationFormat.Docstring,
            "markdown" => CiirDocumentationFormat.Markdown,
            "plain" => CiirDocumentationFormat.Plain,
            _ => CiirDocumentationFormat.Unknown,
        };

    public override void Write(Utf8JsonWriter writer, CiirDocumentationFormat value, JsonSerializerOptions options) =>
        writer.WriteStringValue(value switch
        {
            CiirDocumentationFormat.XmlDoc => "xml-doc",
            CiirDocumentationFormat.Javadoc => "javadoc",
            CiirDocumentationFormat.Jsdoc => "jsdoc",
            CiirDocumentationFormat.Tsdoc => "tsdoc",
            CiirDocumentationFormat.Docstring => "docstring",
            CiirDocumentationFormat.Markdown => "markdown",
            CiirDocumentationFormat.Plain => "plain",
            _ => "unknown",
        });
}
