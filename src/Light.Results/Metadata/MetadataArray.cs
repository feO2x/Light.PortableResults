using System;
using System.Collections;
using System.Collections.Generic;

namespace Light.Results.Metadata;

/// <summary>
/// Represents an immutable array of <see cref="MetadataValue" /> items.
/// </summary>
public readonly struct MetadataArray : IReadOnlyList<MetadataValue>, IEquatable<MetadataArray>
{
    /// <summary>
    /// String representation for an empty array.
    /// </summary>
    public const string EmptyArrayStringRepresentation = "[]";

    internal readonly MetadataArrayData? Data;

    /// <summary>
    /// Gets an empty <see cref="MetadataArray" />.
    /// </summary>
    public static MetadataArray Empty => new (MetadataArrayData.Empty);

    internal MetadataArray(MetadataArrayData data) => Data = data;

    /// <summary>
    /// Gets the number of items in the array.
    /// </summary>
    public int Count => Data?.Count ?? 0;

    /// <summary>
    /// Gets the value at the specified index.
    /// </summary>
    /// <param name="index">The index of the value to retrieve.</param>
    /// <returns>The value at the specified index.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="index" /> is out of range.</exception>
    public MetadataValue this[int index] =>
        Data?[index] ?? throw new ArgumentOutOfRangeException(nameof(index));

    /// <summary>
    /// Returns a read-only span over the values in this array.
    /// </summary>
    /// <returns>A read-only span over the values.</returns>
    public ReadOnlySpan<MetadataValue> AsSpan() => Data is not null ? Data.AsSpan() : ReadOnlySpan<MetadataValue>.Empty;

    /// <summary>
    /// Creates a <see cref="MetadataArray" /> from the specified values.
    /// </summary>
    /// <param name="values">The values to include.</param>
    /// <returns>The created array.</returns>
    public static MetadataArray Create(params MetadataValue[]? values)
    {
        if (values is null || values.Length == 0)
        {
            return Empty;
        }

        return new MetadataArray(new MetadataArrayData(values));
    }

    /// <summary>
    /// Gets the value indicating whether this array only contains primitive values.
    /// </summary>
    public bool HasOnlyPrimitiveChildren => Data?.HasOnlyPrimitiveChildren ?? true;

    /// <summary>
    /// Gets a value-type enumerator for the array.
    /// </summary>
    /// <returns>The enumerator.</returns>
    public Enumerator GetEnumerator() => new (this);

    /// <summary>
    /// Gets a reference-type enumerator for the array.
    /// </summary>
    /// <returns>The enumerator.</returns>
    IEnumerator<MetadataValue> IEnumerable<MetadataValue>.GetEnumerator() => GetEnumerator();

    /// <summary>
    /// Gets a non-generic enumerator for the array.
    /// </summary>
    /// <returns>The enumerator.</returns>
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    /// <summary>
    /// Determines whether this instance and another specified <see cref="MetadataArray" /> have the same value.
    /// </summary>
    /// <param name="other">The other array to compare.</param>
    /// <returns><see langword="true" /> if the arrays are equal; otherwise, <see langword="false" />.</returns>
    public bool Equals(MetadataArray other)
    {
        if (Data is not null && other.Data is not null)
        {
            return Data.Equals(other.Data);
        }

        return ReferenceEquals(Data, other.Data);
    }

    /// <summary>
    /// Determines whether this instance and a specified object have the same value.
    /// </summary>
    /// <param name="obj">The object to compare.</param>
    /// <returns><see langword="true" /> if the objects are equal; otherwise, <see langword="false" />.</returns>
    public override bool Equals(object? obj) => obj is MetadataArray other && Equals(other);

    /// <summary>
    /// Gets the hash code for this instance.
    /// </summary>
    /// <returns>The hash code.</returns>
    public override int GetHashCode() => Data?.GetHashCode() ?? 0;

    /// <summary>
    /// Determines whether two <see cref="MetadataArray" /> instances are equal.
    /// </summary>
    /// <param name="left">The left instance.</param>
    /// <param name="right">The right instance.</param>
    /// <returns><see langword="true" /> if the instances are equal; otherwise, <see langword="false" />.</returns>
    public static bool operator ==(MetadataArray left, MetadataArray right) => left.Equals(right);

    /// <summary>
    /// Determines whether two <see cref="MetadataArray" /> instances are not equal.
    /// </summary>
    /// <param name="left">The left instance.</param>
    /// <param name="right">The right instance.</param>
    /// <returns><see langword="true" /> if the instances are not equal; otherwise, <see langword="false" />.</returns>
    public static bool operator !=(MetadataArray left, MetadataArray right) => !left.Equals(right);

    /// <summary>
    /// Returns the string representation of this instance.
    /// </summary>
    /// <returns>The string representation.</returns>
    public override string ToString() => Data?.ToString() ?? EmptyArrayStringRepresentation;

    /// <summary>
    /// Value-type enumerator for <see cref="MetadataArray" />.
    /// </summary>
    public struct Enumerator : IEnumerator<MetadataValue>
    {
        private readonly MetadataArrayData? _data;
        private int _index;

        internal Enumerator(MetadataArray array)
        {
            _data = array.Data;
            _index = -1;
        }

        /// <summary>
        /// Gets the current element.
        /// </summary>
        /// <exception cref="ArgumentOutOfRangeException">
        /// Thrown when the enumerator is positioned before the first element or after the last element.
        /// </exception>
        /// <exception cref="NullReferenceException">
        /// Thrown when the enumerator is created for an empty array and <see cref="Current" /> is accessed.
        /// </exception>
        public MetadataValue Current => _data![_index];

        /// <summary>
        /// Gets the current element.
        /// </summary>
        object IEnumerator.Current => Current;

        /// <summary>
        /// Advances the enumerator to the next element.
        /// </summary>
        /// <returns>
        /// <see langword="true" /> if the enumerator was successfully advanced; otherwise, <see langword="false" />.
        /// </returns>
        public bool MoveNext()
        {
            if (_data is null)
            {
                return false;
            }

            _index++;
            return _index < _data.Count;
        }

        /// <summary>
        /// Sets the enumerator to its initial position.
        /// </summary>
        public void Reset() => _index = -1;

        /// <summary>
        /// Releases resources used by the enumerator. This is a no-op.
        /// </summary>
        public void Dispose() { }
    }
}
