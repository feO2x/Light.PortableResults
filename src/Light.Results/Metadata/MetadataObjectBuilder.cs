using System;
using System.Buffers;
using System.Collections.Generic;

namespace Light.Results.Metadata;

/// <summary>
/// Builder for creating <see cref="MetadataObject" /> instances efficiently using pooled buffers.
/// </summary>
public struct MetadataObjectBuilder : IDisposable
{
    private const int DefaultCapacity = 4;

    private string[]? _keys;
    private MetadataValue[]? _values;
    private bool _built;

    public int Count { get; private set; }

    public static MetadataObjectBuilder Create(int capacity = DefaultCapacity)
    {
        var actualCapacity = Math.Max(capacity, DefaultCapacity);
        var builder = new MetadataObjectBuilder
        {
            _keys = ArrayPool<string>.Shared.Rent(actualCapacity),
            _values = ArrayPool<MetadataValue>.Shared.Rent(actualCapacity),
            Count = 0,
            _built = false
        };
        return builder;
    }

    public static MetadataObjectBuilder From(MetadataObject source)
    {
        if (source.Data is null || source.Count == 0)
        {
            return Create();
        }

        var builder = Create(source.Count);
        var sourceKeys = source.Data.GetKeys();
        var sourceValues = source.Data.GetValues();

        Array.Copy(sourceKeys, builder._keys!, sourceKeys.Length);
        Array.Copy(sourceValues, builder._values!, sourceValues.Length);
        builder.Count = source.Count;

        return builder;
    }

    public void Add(string key, MetadataValue value)
    {
        ThrowIfBuilt();

        if (key is null)
        {
            throw new ArgumentNullException(nameof(key));
        }

        // Find insertion position using binary search
        var insertIndex = FindInsertionIndex(key);
        if (insertIndex < 0)
        {
            throw new ArgumentException($"Duplicate key: '{key}'.", nameof(key));
        }

        EnsureCapacity(Count + 1);

        // Shift elements to make room for the new key-value pair
        if (insertIndex < Count)
        {
            Array.Copy(_keys!, insertIndex, _keys!, insertIndex + 1, Count - insertIndex);
            Array.Copy(_values!, insertIndex, _values!, insertIndex + 1, Count - insertIndex);
        }

        _keys![insertIndex] = key;
        _values![insertIndex] = value;
        Count++;
    }

    public bool TryGetValue(string key, out MetadataValue value)
    {
        if (key is null)
        {
            throw new ArgumentNullException(nameof(key));
        }

        var index = FindIndex(key);
        if (index >= 0)
        {
            value = _values![index];
            return true;
        }

        value = default;
        return false;
    }

    public bool ContainsKey(string key)
    {
        if (key is null)
        {
            throw new ArgumentNullException(nameof(key));
        }

        return FindIndex(key) >= 0;
    }

    public void Replace(string key, MetadataValue value)
    {
        ThrowIfBuilt();

        if (key is null)
        {
            throw new ArgumentNullException(nameof(key));
        }

        var index = FindIndex(key);
        if (index < 0)
        {
            throw new KeyNotFoundException($"Key '{key}' not found.");
        }

        _values![index] = value;
    }

    public void AddOrReplace(string key, MetadataValue value)
    {
        ThrowIfBuilt();

        if (key is null)
        {
            throw new ArgumentNullException(nameof(key));
        }

        var insertIndex = FindInsertionIndex(key);
        if (insertIndex < 0)
        {
            // Key exists, replace value
            var existingIndex = ~insertIndex;
            _values![existingIndex] = value;
            return;
        }

        // Insert new key-value pair in sorted position
        EnsureCapacity(Count + 1);

        if (insertIndex < Count)
        {
            Array.Copy(_keys!, insertIndex, _keys!, insertIndex + 1, Count - insertIndex);
            Array.Copy(_values!, insertIndex, _values!, insertIndex + 1, Count - insertIndex);
        }

        _keys![insertIndex] = key;
        _values![insertIndex] = value;
        Count++;
    }

    public MetadataObject Build()
    {
        ThrowIfBuilt();
        _built = true;

        if (Count == 0)
        {
            ReturnBuffers();
            return MetadataObject.Empty;
        }

        var keys = new string[Count];
        var values = new MetadataValue[Count];

        Array.Copy(_keys!, keys, Count);
        Array.Copy(_values!, values, Count);

        // Keys are already sorted due to sorted insertion in Add/AddOrReplace
        ReturnBuffers();

        return new MetadataObject(new MetadataObjectData(keys, values));
    }

    public void Dispose()
    {
        if (!_built)
        {
            ReturnBuffers();
            _built = true;
        }
    }

    private void EnsureCapacity(int required)
    {
        if (_keys is null || _keys.Length >= required)
        {
            if (_keys is null)
            {
                var capacity = Math.Max(required, DefaultCapacity);
                _keys = ArrayPool<string>.Shared.Rent(capacity);
                _values = ArrayPool<MetadataValue>.Shared.Rent(capacity);
            }

            return;
        }

        var newCapacity = Math.Max(_keys.Length * 2, required);
        var newKeys = ArrayPool<string>.Shared.Rent(newCapacity);
        var newValues = ArrayPool<MetadataValue>.Shared.Rent(newCapacity);

        Array.Copy(_keys, newKeys, Count);
        Array.Copy(_values!, newValues, Count);

        ArrayPool<string>.Shared.Return(_keys, clearArray: true);
        ArrayPool<MetadataValue>.Shared.Return(_values!, clearArray: true);

        _keys = newKeys;
        _values = newValues;
    }

    private void ReturnBuffers()
    {
        if (_keys is not null)
        {
            ArrayPool<string>.Shared.Return(_keys, clearArray: true);
            _keys = null;
        }

        if (_values is not null)
        {
            ArrayPool<MetadataValue>.Shared.Return(_values, clearArray: true);
            _values = null;
        }
    }

    private void ThrowIfBuilt()
    {
        if (_built)
        {
            throw new InvalidOperationException("Builder has already been used to build an object.");
        }
    }

    /// <summary>
    /// Finds the index of an existing key using binary search.
    /// Returns the index if found, or -1 if not found.
    /// </summary>
    private int FindIndex(string key)
    {
        var lo = 0;
        var hi = Count - 1;

        while (lo <= hi)
        {
            var mid = lo + ((hi - lo) >> 1);
            var cmp = string.CompareOrdinal(_keys![mid], key);

            if (cmp == 0)
            {
                return mid;
            }

            if (cmp < 0)
            {
                lo = mid + 1;
            }
            else
            {
                hi = mid - 1;
            }
        }

        return -1;
    }

    /// <summary>
    /// Finds the insertion index for a new key using binary search.
    /// Returns the insertion index if the key doesn't exist.
    /// Returns a negative value (bitwise complement of existing index) if the key already exists.
    /// </summary>
    private int FindInsertionIndex(string key)
    {
        var lo = 0;
        var hi = Count - 1;

        while (lo <= hi)
        {
            var mid = lo + ((hi - lo) >> 1);
            var cmp = string.CompareOrdinal(_keys![mid], key);

            if (cmp == 0)
            {
                // Key already exists, return negative value
                return ~mid;
            }

            if (cmp < 0)
            {
                lo = mid + 1;
            }
            else
            {
                hi = mid - 1;
            }
        }

        return lo;
    }
}
