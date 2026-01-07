using System;

namespace Light.Results.Metadata;

/// <summary>
/// Represents a key-value pair in a <see cref="MetadataObject" />.
/// This struct is used internally for single-array storage to improve iteration cache locality.
/// </summary>
internal readonly struct MetadataEntry
{
    public MetadataEntry(string key, MetadataValue value)
    {
        Key = key ?? throw new ArgumentNullException(nameof(key));
        Value = value;
    }

    public string Key { get; }
    public MetadataValue Value { get; }

    public void Deconstruct(out string key, out MetadataValue value)
    {
        key = Key;
        value = Value;
    }
}
