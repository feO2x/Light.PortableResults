using System;
using System.Collections;
using System.Collections.Generic;

namespace Light.Results.Metadata;

/// <summary>
/// Represents an immutable object with string keys and <see cref="MetadataValue" /> values.
/// Properties are stored in insertion order.
/// </summary>
public readonly struct MetadataObject : IReadOnlyDictionary<string, MetadataValue>, IEquatable<MetadataObject>
{
    /// <summary>
    /// String representation for an empty object.
    /// </summary>
    public const string EmptyObjectStringRepresentation = "{}";

    internal readonly MetadataObjectData? Data;

    /// <summary>
    /// Gets an empty <see cref="MetadataObject" />.
    /// </summary>
    public static MetadataObject Empty => new (MetadataObjectData.Empty);

    internal MetadataObject(MetadataObjectData data) => Data = data;

    /// <summary>
    /// Gets the number of entries in the object.
    /// </summary>
    public int Count => Data?.Count ?? 0;

    /// <summary>
    /// Gets the value for the specified key.
    /// </summary>
    /// <param name="key">The key to look up.</param>
    /// <returns>The value associated with the specified key.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="key" /> is <see langword="null" />.</exception>
    /// <exception cref="KeyNotFoundException">Thrown when the key does not exist.</exception>
    public MetadataValue this[string key] =>
        TryGetValue(key, out var value) ?
            value :
            throw new KeyNotFoundException($"Key '{key}' was not found in MetadataObject");

    /// <summary>
    /// Gets an enumerable collection of keys.
    /// </summary>
    public IEnumerable<string> Keys
    {
        get
        {
            if (Data is null)
            {
                yield break;
            }

            for (var i = 0; i < Data.Count; i++)
            {
                yield return Data.GetEntry(i).Key;
            }
        }
    }

    /// <summary>
    /// Gets an enumerable collection of values.
    /// </summary>
    public IEnumerable<MetadataValue> Values
    {
        get
        {
            if (Data is null)
            {
                yield break;
            }

            for (var i = 0; i < Data.Count; i++)
            {
                yield return Data.GetEntry(i).Value;
            }
        }
    }

    /// <summary>
    /// Gets the value indicating whether this object only contains primitive values.
    /// Uses the <see cref="MetadataKindExtensions.IsPrimitive" /> method internally.
    /// </summary>
    public bool HasOnlyPrimitiveChildren => Data?.HasOnlyPrimitiveChildren ?? true;

    /// <summary>
    /// Determines whether the object contains the specified key.
    /// </summary>
    /// <param name="key">The key to look up.</param>
    /// <returns><see langword="true" /> if the key exists; otherwise, <see langword="false" />.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="key" /> is <see langword="null" />.</exception>
    public bool ContainsKey(string key)
    {
        if (key is null)
        {
            throw new ArgumentNullException(nameof(key));
        }

        return Data is not null && Data.TryGetValue(key, out _);
    }

    /// <summary>
    /// Attempts to get the value for the specified key.
    /// </summary>
    /// <param name="key">The key to look up.</param>
    /// <param name="value">When this method returns, contains the value if the key was found.</param>
    /// <returns><see langword="true" /> if the key was found; otherwise, <see langword="false" />.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="key" /> is <see langword="null" />.</exception>
    public bool TryGetValue(string key, out MetadataValue value)
    {
        if (key is null)
        {
            throw new ArgumentNullException(nameof(key));
        }

        if (Data is not null)
        {
            return Data.TryGetValue(key, out value);
        }

        value = default;
        return false;
    }

    // Typed getters for convenience
    /// <summary>
    /// Attempts to get a boolean value for the specified key.
    /// </summary>
    /// <param name="key">The key to look up.</param>
    /// <param name="value">When this method returns, contains the boolean value if found.</param>
    /// <returns><see langword="true" /> if the key exists and the value is a boolean; otherwise, <see langword="false" />.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="key" /> is <see langword="null" />.</exception>
    public bool TryGetBoolean(string key, out bool value)
    {
        if (TryGetValue(key, out var mv) && mv.TryGetBoolean(out value))
        {
            return true;
        }

        value = false;
        return false;
    }

    /// <summary>
    /// Attempts to get an int64 value for the specified key.
    /// </summary>
    /// <param name="key">The key to look up.</param>
    /// <param name="value">When this method returns, contains the int64 value if found.</param>
    /// <returns><see langword="true" /> if the key exists and the value is an int64; otherwise, <see langword="false" />.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="key" /> is <see langword="null" />.</exception>
    public bool TryGetInt64(string key, out long value)
    {
        if (TryGetValue(key, out var metadataValue) && metadataValue.TryGetInt64(out value))
        {
            return true;
        }

        value = 0;
        return false;
    }

    /// <summary>
    /// Attempts to get a double value for the specified key.
    /// </summary>
    /// <param name="key">The key to look up.</param>
    /// <param name="value">When this method returns, contains the double value if found.</param>
    /// <returns><see langword="true" /> if the key exists and the value is a double; otherwise, <see langword="false" />.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="key" /> is <see langword="null" />.</exception>
    public bool TryGetDouble(string key, out double value)
    {
        if (TryGetValue(key, out var metadataValue) && metadataValue.TryGetDouble(out value))
        {
            return true;
        }

        value = 0.0;
        return false;
    }

    /// <summary>
    /// Attempts to get a string value for the specified key.
    /// </summary>
    /// <param name="key">The key to look up.</param>
    /// <param name="value">When this method returns, contains the string value if found.</param>
    /// <returns><see langword="true" /> if the key exists and the value is a string; otherwise, <see langword="false" />.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="key" /> is <see langword="null" />.</exception>
    public bool TryGetString(string key, out string? value)
    {
        if (TryGetValue(key, out var metadataValue) && metadataValue.TryGetString(out value))
        {
            return true;
        }

        value = null;
        return false;
    }

    /// <summary>
    /// Attempts to get a decimal value for the specified key.
    /// </summary>
    /// <param name="key">The key to look up.</param>
    /// <param name="value">When this method returns, contains the decimal value if found.</param>
    /// <returns><see langword="true" /> if the key exists and the value is a decimal; otherwise, <see langword="false" />.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="key" /> is <see langword="null" />.</exception>
    public bool TryGetDecimal(string key, out decimal value)
    {
        if (TryGetValue(key, out var metadataValue) && metadataValue.TryGetDecimal(out value))
        {
            return true;
        }

        value = 0;
        return false;
    }

    /// <summary>
    /// Attempts to get a <see cref="MetadataArray" /> value for the specified key.
    /// </summary>
    /// <param name="key">The key to look up.</param>
    /// <param name="value">When this method returns, contains the array value if found.</param>
    /// <returns><see langword="true" /> if the key exists and the value is an array; otherwise, <see langword="false" />.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="key" /> is <see langword="null" />.</exception>
    public bool TryGetArray(string key, out MetadataArray value)
    {
        if (TryGetValue(key, out var metadataValue) && metadataValue.TryGetArray(out value))
        {
            return true;
        }

        value = default;
        return false;
    }

    /// <summary>
    /// Attempts to get a <see cref="MetadataObject" /> value for the specified key.
    /// </summary>
    /// <param name="key">The key to look up.</param>
    /// <param name="value">When this method returns, contains the object value if found.</param>
    /// <returns><see langword="true" /> if the key exists and the value is an object; otherwise, <see langword="false" />.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="key" /> is <see langword="null" />.</exception>
    public bool TryGetObject(string key, out MetadataObject value)
    {
        if (TryGetValue(key, out var metadataValue) && metadataValue.TryGetObject(out value))
        {
            return true;
        }

        value = default;
        return false;
    }

    /// <summary>
    /// Creates a <see cref="MetadataObject" /> from the specified properties.
    /// </summary>
    /// <param name="properties">The properties to include.</param>
    /// <returns>The created object.</returns>
    /// <exception cref="ArgumentNullException">Thrown when a property key is <see langword="null" />.</exception>
    public static MetadataObject Create(params (string Key, MetadataValue Value)[]? properties) =>
        Create(null, properties);

    /// <summary>
    /// Creates a <see cref="MetadataObject" /> from the specified properties.
    /// </summary>
    /// <param name="keyComparer">The key comparer used for lookups.</param>
    /// <param name="properties">The properties to include.</param>
    /// <returns>The created object.</returns>
    /// <exception cref="ArgumentNullException">Thrown when a property key is <see langword="null" />.</exception>
    public static MetadataObject Create(
        IEqualityComparer<string>? keyComparer,
        params (string Key, MetadataValue Value)[]? properties
    )
    {
        if (properties is null || properties.Length == 0)
        {
            return Empty;
        }

        var entries = new KeyValuePair<string, MetadataValue>[properties.Length];

        for (var i = 0; i < properties.Length; i++)
        {
            var key = properties[i].Key ?? throw new ArgumentNullException(nameof(properties), "Key cannot be null.");
            entries[i] = new KeyValuePair<string, MetadataValue>(key, properties[i].Value);
        }

        return new MetadataObject(new MetadataObjectData(entries, keyComparer));
    }

    /// <summary>
    /// Gets a value-type enumerator for the object.
    /// </summary>
    /// <returns>The enumerator.</returns>
    public Enumerator GetEnumerator() => new (this);

    /// <summary>
    /// Gets a reference-type enumerator for the object.
    /// </summary>
    /// <returns>The enumerator.</returns>
    IEnumerator<KeyValuePair<string, MetadataValue>> IEnumerable<KeyValuePair<string, MetadataValue>>.GetEnumerator() =>
        GetEnumerator();

    /// <summary>
    /// Gets a non-generic enumerator for the object.
    /// </summary>
    /// <returns>The enumerator.</returns>
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    /// <summary>
    /// Determines whether this instance and another specified <see cref="MetadataObject" /> have the same value.
    /// </summary>
    /// <param name="other">The other object to compare.</param>
    /// <returns><see langword="true" /> if the objects are equal; otherwise, <see langword="false" />.</returns>
    public bool Equals(MetadataObject other)
    {
        if (Data is null && other.Data is null)
        {
            return true;
        }

        if (Data is null || other.Data is null)
        {
            return false;
        }

        return Data.Equals(other.Data);
    }

    /// <summary>
    /// Determines whether this instance and a specified object have the same value.
    /// </summary>
    /// <param name="obj">The object to compare.</param>
    /// <returns><see langword="true" /> if the objects are equal; otherwise, <see langword="false" />.</returns>
    public override bool Equals(object? obj) => obj is MetadataObject other && Equals(other);

    /// <summary>
    /// Gets the hash code for this instance.
    /// </summary>
    /// <returns>The hash code.</returns>
    public override int GetHashCode() => Data?.GetHashCode() ?? 0;

    /// <summary>
    /// Determines whether two <see cref="MetadataObject" /> instances are equal.
    /// </summary>
    /// <param name="left">The left instance.</param>
    /// <param name="right">The right instance.</param>
    /// <returns><see langword="true" /> if the instances are equal; otherwise, <see langword="false" />.</returns>
    public static bool operator ==(MetadataObject left, MetadataObject right) => left.Equals(right);

    /// <summary>
    /// Determines whether two <see cref="MetadataObject" /> instances are not equal.
    /// </summary>
    /// <param name="left">The left instance.</param>
    /// <param name="right">The right instance.</param>
    /// <returns><see langword="true" /> if the instances are not equal; otherwise, <see langword="false" />.</returns>
    public static bool operator !=(MetadataObject left, MetadataObject right) => !left.Equals(right);

    /// <summary>
    /// Returns the string representation of this instance.
    /// </summary>
    /// <returns>The string representation.</returns>
    public override string ToString() => Data?.ToString() ?? EmptyObjectStringRepresentation;

    /// <summary>
    /// Value-type enumerator for <see cref="MetadataObject" />.
    /// </summary>
    public struct Enumerator : IEnumerator<KeyValuePair<string, MetadataValue>>
    {
        private readonly KeyValuePair<string, MetadataValue>[]? _entries;
        private readonly int _count;
        private int _index;

        internal Enumerator(MetadataObject @object)
        {
            if (@object.Data is null)
            {
                _entries = null;
                _count = 0;
            }
            else
            {
                _entries = @object.Data.GetEntries();
                _count = @object.Data.Count;
            }

            _index = -1;
        }

        /// <summary>
        /// Gets the current element.
        /// </summary>
        /// <exception cref="ArgumentOutOfRangeException">
        /// Thrown when the enumerator is positioned before the first element or after the last element.
        /// </exception>
        /// <exception cref="NullReferenceException">
        /// Thrown when the enumerator is created for an empty object and <see cref="Current" /> is accessed.
        /// </exception>
        public KeyValuePair<string, MetadataValue> Current => _entries![_index];

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
            _index++;
            return _index < _count;
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
