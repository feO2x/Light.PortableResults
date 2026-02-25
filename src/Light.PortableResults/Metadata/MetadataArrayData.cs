using System;

namespace Light.PortableResults.Metadata;

/// <summary>
/// Internal backing storage for <see cref="MetadataArray" />. Owns the values array.
/// </summary>
internal sealed class MetadataArrayData : IEquatable<MetadataArrayData>
{
    public static readonly MetadataArrayData Empty = new ([]);

    private readonly MetadataValue[] _values;
    private string? _cachedToStringRepresentation;
    private bool? _hasOnlyPrimitiveChildren;

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

    public bool HasOnlyPrimitiveChildren
    {
        get
        {
            if (_hasOnlyPrimitiveChildren.HasValue)
            {
                return _hasOnlyPrimitiveChildren.Value;
            }

            foreach (var value in _values)
            {
                if (!value.Kind.IsPrimitive())
                {
                    _hasOnlyPrimitiveChildren = false;
                    return false;
                }
            }

            _hasOnlyPrimitiveChildren = true;
            return true;
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

    public override string ToString() => _cachedToStringRepresentation ??= CreateStringRepresentation(_values);

    private static string CreateStringRepresentation(MetadataValue[] values) =>
        values.Length switch
        {
            0 => MetadataArray.EmptyArrayStringRepresentation,
            1 => $"[{values[0].ToString()}]",
            2 => $"[{values[0].ToString()}, {values[1].ToString()}]",
            3 => $"[{values[0].ToString()}, {values[1].ToString()}, {values[2].ToString()}]",
            4 => $"[{values[0].ToString()}, {values[1].ToString()}, {values[2].ToString()}, {values[3].ToString()}]",
            5 =>
                $"[{values[0].ToString()}, {values[1].ToString()}, {values[2].ToString()}, {values[3].ToString()}, {values[4].ToString()}]",
            _ => $"[{string.Join(", ", values)}]"
        };
}
