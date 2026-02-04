using System;
using System.Buffers;
using System.Collections.Generic;

namespace Light.Results.Metadata;

/// <summary>
/// Builder for creating <see cref="MetadataObject" /> instances efficiently using pooled buffers.
/// Entries are stored in insertion order.
/// </summary>
public struct MetadataObjectBuilder : IDisposable
{
    private const int DefaultCapacity = 4;
    private const int DictionaryThreshold = 10;

    private KeyValuePair<string, MetadataValue>[]? _entries;
    private Dictionary<string, int>? _indexLookup;
    private IEqualityComparer<string>? _keyComparer;
    private bool _built;

    /// <summary>
    /// Gets the number of entries added to the builder.
    /// </summary>
    public int Count { get; private set; }

    /// <summary>
    /// Creates a new <see cref="MetadataObjectBuilder" /> with the specified initial capacity.
    /// </summary>
    /// <param name="capacity">The initial capacity.</param>
    /// <returns>The builder.</returns>
    public static MetadataObjectBuilder Create(int capacity = DefaultCapacity)
    {
        var actualCapacity = Math.Max(capacity, DefaultCapacity);
        var builder = new MetadataObjectBuilder
        {
            _entries = ArrayPool<KeyValuePair<string, MetadataValue>>.Shared.Rent(actualCapacity),
            Count = 0,
            _built = false
        };
        return builder;
    }

    /// <summary>
    /// Creates a builder populated with the entries from the specified <see cref="MetadataObject" />.
    /// </summary>
    /// <param name="source">The source object.</param>
    /// <returns>The builder.</returns>
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

    /// <summary>
    /// Adds a key/value entry to the builder.
    /// </summary>
    /// <param name="key">The entry key.</param>
    /// <param name="value">The entry value.</param>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the builder has already been used to build an object.
    /// </exception>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="key" /> is <see langword="null" />.</exception>
    /// <exception cref="ArgumentException">Thrown when the key already exists.</exception>
    public void Add(string key, MetadataValue value)
    {
        ThrowIfBuilt();

        if (key is null)
        {
            throw new ArgumentNullException(nameof(key));
        }

        if (ContainsKey(key))
        {
            throw new ArgumentException($"Duplicate key: '{key}'.", nameof(key));
        }

        EnsureCapacity(Count + 1);

        _entries![Count] = new KeyValuePair<string, MetadataValue>(key, value);

        // Update dictionary if it exists
        if (_indexLookup is not null)
        {
            _indexLookup[key] = Count;
        }

        Count++;
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

        var index = FindIndex(key);
        if (index >= 0)
        {
            value = _entries![index].Value;
            return true;
        }

        value = default;
        return false;
    }

    /// <summary>
    /// Determines whether the builder contains the specified key.
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

        return FindIndex(key) >= 0;
    }

    /// <summary>
    /// Replaces the value for an existing key.
    /// </summary>
    /// <param name="key">The key to replace.</param>
    /// <param name="value">The new value.</param>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the builder has already been used to build an object.
    /// </exception>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="key" /> is <see langword="null" />.</exception>
    /// <exception cref="KeyNotFoundException">Thrown when the key does not exist.</exception>
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

        _entries![index] = new KeyValuePair<string, MetadataValue>(key, value);
    }

    /// <summary>
    /// Adds a key/value entry or replaces the value if the key already exists.
    /// </summary>
    /// <param name="key">The entry key.</param>
    /// <param name="value">The entry value.</param>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the builder has already been used to build an object.
    /// </exception>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="key" /> is <see langword="null" />.</exception>
    public void AddOrReplace(string key, MetadataValue value)
    {
        ThrowIfBuilt();

        if (key is null)
        {
            throw new ArgumentNullException(nameof(key));
        }

        var existingIndex = FindIndex(key);
        if (existingIndex >= 0)
        {
            // Key exists, replace value
            _entries![existingIndex] = new KeyValuePair<string, MetadataValue>(key, value);
            return;
        }

        // Append new entry
        EnsureCapacity(Count + 1);

        _entries![Count] = new KeyValuePair<string, MetadataValue>(key, value);

        // Update dictionary if it exists
        if (_indexLookup is not null)
        {
            _indexLookup[key] = Count;
        }

        Count++;
    }

    /// <summary>
    /// Sets the comparer used for key lookup.
    /// </summary>
    /// <param name="keyComparer">The comparer to use.</param>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the builder has already been used to build an object.
    /// </exception>
    public void SetKeyComparer(IEqualityComparer<string> keyComparer)
    {
        ThrowIfBuilt();
        _keyComparer = keyComparer;
    }

    /// <summary>
    /// Builds a <see cref="MetadataObject" /> from the collected entries.
    /// </summary>
    /// <returns>The created object.</returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the builder has already been used to build an object.
    /// </exception>
    public MetadataObject Build()
    {
        ThrowIfBuilt();
        _built = true;

        if (Count == 0)
        {
            ReturnBuffer();
            return MetadataObject.Empty;
        }

        var entries = new KeyValuePair<string, MetadataValue>[Count];
        Array.Copy(_entries!, entries, Count);

        // Entries are already sorted due to sorted insertion in Add/AddOrReplace
        ReturnBuffer();

        return new MetadataObject(new MetadataObjectData(entries, _keyComparer));
    }

    /// <summary>
    /// Returns the pooled buffer to the pool if it has not already been returned.
    /// </summary>
    public void Dispose()
    {
        if (_built)
        {
            return;
        }

        ReturnBuffer();
        _built = true;
    }

    private void EnsureCapacity(int required)
    {
        if (_entries is null || _entries.Length >= required)
        {
            if (_entries is null)
            {
                var capacity = Math.Max(required, DefaultCapacity);
                _entries = ArrayPool<KeyValuePair<string, MetadataValue>>.Shared.Rent(capacity);
            }

            return;
        }

        var newCapacity = Math.Max(_entries.Length * 2, required);
        var newEntries = ArrayPool<KeyValuePair<string, MetadataValue>>.Shared.Rent(newCapacity);

        Array.Copy(_entries, newEntries, Count);

        ArrayPool<KeyValuePair<string, MetadataValue>>.Shared.Return(_entries, clearArray: true);

        _entries = newEntries;
    }

    private void ReturnBuffer()
    {
        if (_entries is not null)
        {
            ArrayPool<KeyValuePair<string, MetadataValue>>.Shared.Return(_entries, clearArray: true);
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
    /// Finds the index of an existing key using linear search or dictionary lookup.
    /// Returns the index if found, or -1 if not found.
    /// </summary>
    private int FindIndex(string key)
    {
        if (Count == 0)
        {
            return -1;
        }

        if (Count > DictionaryThreshold)
        {
            return FindIndexWithDictionary(key);
        }

        return FindIndexLinear(key);
    }

    private int FindIndexLinear(string key)
    {
        if (_keyComparer is null)
        {
            for (var i = 0; i < Count; i++)
            {
                if (key.Equals(_entries![i].Key, StringComparison.Ordinal))
                {
                    return i;
                }
            }

            return -1;
        }

        for (var i = 0; i < Count; i++)
        {
            if (_keyComparer.Equals(key, _entries![i].Key))
            {
                return i;
            }
        }

        return -1;
    }

    private int FindIndexWithDictionary(string key)
    {
        if (_indexLookup is null)
        {
            _indexLookup = _keyComparer is null ?
                new Dictionary<string, int>(Count) :
                new Dictionary<string, int>(Count, _keyComparer);

            for (var i = 0; i < Count; i++)
            {
                _indexLookup[_entries![i].Key] = i;
            }
        }

        return _indexLookup.TryGetValue(key, out var index) ? index : -1;
    }
}
