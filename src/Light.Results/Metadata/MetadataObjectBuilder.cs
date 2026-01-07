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

    private MetadataEntry[]? _entries;
    private bool _built;

    public int Count { get; private set; }

    public static MetadataObjectBuilder Create(int capacity = DefaultCapacity)
    {
        var actualCapacity = Math.Max(capacity, DefaultCapacity);
        var builder = new MetadataObjectBuilder
        {
            _entries = ArrayPool<MetadataEntry>.Shared.Rent(actualCapacity),
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
        var sourceEntries = source.Data.GetEntries();

        Array.Copy(sourceEntries, builder._entries!, sourceEntries.Length);
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

        // Shift elements to make room for the new entry
        if (insertIndex < Count)
        {
            Array.Copy(_entries!, insertIndex, _entries!, insertIndex + 1, Count - insertIndex);
        }

        _entries![insertIndex] = new MetadataEntry(key, value);
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
            value = _entries![index].Value;
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

        _entries![index] = new MetadataEntry(key, value);
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
            _entries![existingIndex] = new MetadataEntry(key, value);
            return;
        }

        // Insert new entry in sorted position
        EnsureCapacity(Count + 1);

        if (insertIndex < Count)
        {
            Array.Copy(_entries!, insertIndex, _entries!, insertIndex + 1, Count - insertIndex);
        }

        _entries![insertIndex] = new MetadataEntry(key, value);
        Count++;
    }

    public MetadataObject Build()
    {
        ThrowIfBuilt();
        _built = true;

        if (Count == 0)
        {
            ReturnBuffer();
            return MetadataObject.Empty;
        }

        var entries = new MetadataEntry[Count];
        Array.Copy(_entries!, entries, Count);

        // Entries are already sorted due to sorted insertion in Add/AddOrReplace
        ReturnBuffer();

        return new MetadataObject(new MetadataObjectData(entries));
    }

    public void Dispose()
    {
        if (!_built)
        {
            ReturnBuffer();
            _built = true;
        }
    }

    private void EnsureCapacity(int required)
    {
        if (_entries is null || _entries.Length >= required)
        {
            if (_entries is null)
            {
                var capacity = Math.Max(required, DefaultCapacity);
                _entries = ArrayPool<MetadataEntry>.Shared.Rent(capacity);
            }

            return;
        }

        var newCapacity = Math.Max(_entries.Length * 2, required);
        var newEntries = ArrayPool<MetadataEntry>.Shared.Rent(newCapacity);

        Array.Copy(_entries, newEntries, Count);

        ArrayPool<MetadataEntry>.Shared.Return(_entries, clearArray: true);

        _entries = newEntries;
    }

    private void ReturnBuffer()
    {
        if (_entries is not null)
        {
            ArrayPool<MetadataEntry>.Shared.Return(_entries, clearArray: true);
            _entries = null;
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
            var cmp = string.CompareOrdinal(_entries![mid].Key, key);

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
            var cmp = string.CompareOrdinal(_entries![mid].Key, key);

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
