namespace Ciir.Core.Configuration;

/// <summary>The <c>configuration_key</c>-specific details of a CIIR document whose <c>kind</c> is <see cref="CiirKind.ConfigurationKey"/>.</summary>
public sealed record CiirConfigurationKeyInfo
{
    /// <summary>The JSON type of the value at this key path. The value itself is never captured.</summary>
    public required CiirConfigurationValueKind ValueType { get; init; }
}
