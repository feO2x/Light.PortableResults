using System;
using System.Collections.Generic;

namespace Light.Results.Metadata;

/// <summary>
/// Internal backing storage for <see cref="MetadataObject" />. Owns a single array of entries,
/// sorted by key for deterministic ordering and better cache locality during iteration.
/// </summary>
internal sealed class MetadataObjectData
{
    private const int DictionaryThreshold = 8;

    private readonly MetadataEntry[] _entries;
    private Dictionary<string, int>? _indexLookup;

    internal MetadataObjectData(MetadataEntry[] entries)
    {
        _entries = entries ?? throw new ArgumentNullException(nameof(entries));
    }

    public static MetadataObjectData Empty { get; } = new ([]);

    public int Count => _entries.Length;

    public ref readonly MetadataEntry GetEntry(int index)
    {
        return ref _entries[index];
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
            value = _entries[index].Value;
            return true;
        }

        value = default;
        return false;
    }

    private int FindIndex(string key)
    {
        return _entries.Length switch
        {
            0 => -1,
            > DictionaryThreshold => FindIndexWithDictionary(key),
            _ => FindIndexLinear(key)
        };
    }

    private int FindIndexLinear(string key)
    {
        for (var i = 0; i < _entries.Length; i++)
        {
            if (string.Equals(_entries[i].Key, key, StringComparison.Ordinal))
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
            var dict = new Dictionary<string, int>(_entries.Length, StringComparer.Ordinal);
            for (var i = 0; i < _entries.Length; i++)
            {
                dict[_entries[i].Key] = i;
            }

            _indexLookup = dict;
        }

        return _indexLookup.TryGetValue(key, out var index) ? index : -1;
    }

    public MetadataEntry[] GetEntries() => _entries;
}
