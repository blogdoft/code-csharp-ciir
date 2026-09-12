using System.Text.Json.Serialization;

namespace Ciir.Core.Configuration;

/// <summary>The JSON value type of a flattened configuration key. Never the value itself.</summary>
public enum CiirConfigurationValueKind
{
    /// <summary>A JSON string.</summary>
    [JsonStringEnumMemberName("string")]
    StringValue,

    /// <summary>A JSON number.</summary>
    [JsonStringEnumMemberName("number")]
    NumberValue,

    /// <summary>A JSON boolean.</summary>
    [JsonStringEnumMemberName("boolean")]
    BooleanValue,

    /// <summary>A JSON array.</summary>
    [JsonStringEnumMemberName("array")]
    ArrayValue,

    /// <summary>A JSON object.</summary>
    [JsonStringEnumMemberName("object")]
    ObjectValue,

    /// <summary>A JSON null.</summary>
    [JsonStringEnumMemberName("null")]
    NullValue,
}
