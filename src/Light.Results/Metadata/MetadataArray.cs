using System;
using System.Collections;
using System.Collections.Generic;

namespace Light.Results.Metadata;

/// <summary>
/// Represents an immutable array of <see cref="MetadataValue" /> items.
/// </summary>
public readonly struct MetadataArray : IReadOnlyList<MetadataValue>, IEquatable<MetadataArray>
{
    internal readonly MetadataArrayData? Data;

    public static MetadataArray Empty => new (MetadataArrayData.Empty);

    internal MetadataArray(MetadataArrayData data)
    {
        Data = data;
    }

    public int Count => Data?.Count ?? 0;

    public MetadataValue this[int index] =>
        Data?[index] ?? throw new ArgumentOutOfRangeException(nameof(index));

    public ReadOnlySpan<MetadataValue> AsSpan() => Data is not null ? Data.AsSpan() : ReadOnlySpan<MetadataValue>.Empty;

    public static MetadataArray Create(params MetadataValue[]? values)
    {
        if (values is null || values.Length == 0)
        {
            return Empty;
        }

        var copy = new MetadataValue[values.Length];
        Array.Copy(values, copy, values.Length);
        return new MetadataArray(new MetadataArrayData(copy));
    }

    public Enumerator GetEnumerator() => new (this);

    IEnumerator<MetadataValue> IEnumerable<MetadataValue>.GetEnumerator() => GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    public bool Equals(MetadataArray other)
    {
        if (Data is not null && other.Data is not null)
        {
            return Data.Equals(other.Data);
        }

        return ReferenceEquals(Data, other.Data);
    }

    public override bool Equals(object? obj) => obj is MetadataArray other && Equals(other);

    public override int GetHashCode() => Data?.GetHashCode() ?? 0;

    public static bool operator ==(MetadataArray left, MetadataArray right) => left.Equals(right);
    public static bool operator !=(MetadataArray left, MetadataArray right) => !left.Equals(right);

    public struct Enumerator : IEnumerator<MetadataValue>
    {
        private readonly MetadataArrayData? _data;
        private int _index;

        internal Enumerator(MetadataArray array)
        {
            _data = array.Data;
            _index = -1;
        }

        public MetadataValue Current => _data![_index];

        object IEnumerator.Current => Current;

        public bool MoveNext()
        {
            if (_data is null)
            {
                return false;
            }

            _index++;
            return _index < _data.Count;
        }

        public void Reset() => _index = -1;

        public void Dispose() { }
    }
}
