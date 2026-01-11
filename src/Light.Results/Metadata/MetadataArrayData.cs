using System;

namespace Light.Results.Metadata;

/// <summary>
/// Internal backing storage for <see cref="MetadataArray" />. Owns the values array.
/// </summary>
internal sealed class MetadataArrayData : IEquatable<MetadataArrayData>
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

    public bool Equals(MetadataArrayData? other)
    {
        if (other is null)
        {
            return false;
        }

        if (Count != other.Count)
        {
            return false;
        }

        return _values.AsSpan().SequenceEqual(other._values.AsSpan());
    }

    public ReadOnlySpan<MetadataValue> AsSpan() => _values;

    public override bool Equals(object? obj) => Equals(obj as MetadataArrayData);

    public override int GetHashCode()
    {
        var hash = new HashCode();
        for (var i = 0; i < _values.Length; i++)
        {
            hash.Add(_values[i]);
        }

        return hash.ToHashCode();
    }
}
