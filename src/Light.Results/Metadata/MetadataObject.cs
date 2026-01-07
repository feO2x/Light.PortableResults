using System;
using System.Collections;
using System.Collections.Generic;

namespace Light.Results.Metadata;

/// <summary>
/// Represents an immutable object with string keys and <see cref="MetadataValue" /> values.
/// Properties are stored in deterministic order (sorted by key).
/// </summary>
public readonly struct MetadataObject : IReadOnlyDictionary<string, MetadataValue>, IEquatable<MetadataObject>
{
    internal readonly MetadataObjectData? Data;

    public static MetadataObject Empty => new (MetadataObjectData.Empty);

    internal MetadataObject(MetadataObjectData data)
    {
        Data = data;
    }

    public int Count => Data?.Count ?? 0;

    public MetadataValue this[string key] =>
        TryGetValue(key, out var value) ? value : throw new KeyNotFoundException($"Key '{key}' not found.");

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
                yield return Data.GetKey(i);
            }
        }
    }

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
                yield return Data.GetValue(i);
            }
        }
    }

    public bool ContainsKey(string key)
    {
        if (key is null)
        {
            throw new ArgumentNullException(nameof(key));
        }

        return Data is not null && Data.TryGetValue(key, out _);
    }

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
    public bool TryGetBoolean(string key, out bool value)
    {
        if (TryGetValue(key, out var mv) && mv.TryGetBoolean(out value))
        {
            return true;
        }

        value = false;
        return false;
    }

    public bool TryGetInt64(string key, out long value)
    {
        if (TryGetValue(key, out var mv) && mv.TryGetInt64(out value))
        {
            return true;
        }

        value = 0;
        return false;
    }

    public bool TryGetDouble(string key, out double value)
    {
        if (TryGetValue(key, out var mv) && mv.TryGetDouble(out value))
        {
            return true;
        }

        value = 0.0;
        return false;
    }

    public bool TryGetString(string key, out string? value)
    {
        if (TryGetValue(key, out var mv) && mv.TryGetString(out value))
        {
            return true;
        }

        value = null;
        return false;
    }

    public bool TryGetDecimal(string key, out decimal value)
    {
        if (TryGetValue(key, out var mv) && mv.TryGetDecimal(out value))
        {
            return true;
        }

        value = 0;
        return false;
    }

    public bool TryGetArray(string key, out MetadataArray value)
    {
        if (TryGetValue(key, out var mv) && mv.TryGetArray(out value))
        {
            return true;
        }

        value = default;
        return false;
    }

    public bool TryGetObject(string key, out MetadataObject value)
    {
        if (TryGetValue(key, out var mv) && mv.TryGetObject(out value))
        {
            return true;
        }

        value = default;
        return false;
    }

    public static MetadataObject Create(params (string Key, MetadataValue Value)[]? properties)
    {
        if (properties is null || properties.Length == 0)
        {
            return Empty;
        }

        var keys = new string[properties.Length];
        var values = new MetadataValue[properties.Length];

        for (var i = 0; i < properties.Length; i++)
        {
            keys[i] = properties[i].Key ?? throw new ArgumentNullException(nameof(properties), "Key cannot be null.");
            values[i] = properties[i].Value;
        }

        // Sort by key for deterministic ordering
        Array.Sort(keys, values, StringComparer.Ordinal);

        // Check for duplicates
        for (var i = 1; i < keys.Length; i++)
        {
            if (string.Equals(keys[i], keys[i - 1], StringComparison.Ordinal))
            {
                throw new ArgumentException($"Duplicate key: '{keys[i]}'.", nameof(properties));
            }
        }

        return new MetadataObject(new MetadataObjectData(keys, values));
    }

    public Enumerator GetEnumerator() => new (this);

    IEnumerator<KeyValuePair<string, MetadataValue>> IEnumerable<KeyValuePair<string, MetadataValue>>.GetEnumerator() =>
        GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    public bool Equals(MetadataObject other) => ReferenceEquals(Data, other.Data);

    public override bool Equals(object? obj) => obj is MetadataObject other && Equals(other);

    public override int GetHashCode() => Data?.GetHashCode() ?? 0;

    public static bool operator ==(MetadataObject left, MetadataObject right) => left.Equals(right);
    public static bool operator !=(MetadataObject left, MetadataObject right) => !left.Equals(right);

    public struct Enumerator : IEnumerator<KeyValuePair<string, MetadataValue>>
    {
        private readonly MetadataObjectData? _data;
        private int _index;

        internal Enumerator(MetadataObject obj)
        {
            _data = obj.Data;
            _index = -1;
        }

        public KeyValuePair<string, MetadataValue> Current =>
            new (_data!.GetKey(_index), _data.GetValue(_index));

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
