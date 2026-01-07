using System;

namespace Light.Results.Metadata;

/// <summary>
/// Internal backing storage for <see cref="MetadataArray" />. Owns the values array.
/// </summary>
internal sealed class MetadataArrayData
{
    public static readonly MetadataArrayData Empty = new ([]);

    private readonly MetadataValue[] _values;

    internal MetadataArrayData(MetadataValue[] values)
    {
        _values = values ?? throw new ArgumentNullException(nameof(values));
    }

    public int Count => _values.Length;

    public MetadataValue this[int index]
    {
        get
        {
            if ((uint) index >= (uint) _values.Length)
            {
                throw new ArgumentOutOfRangeException(nameof(index));
            }

            return _values[index];
        }
    }

    public ReadOnlySpan<MetadataValue> AsSpan() => _values;

    public MetadataValue[] GetValues() => _values;
}
