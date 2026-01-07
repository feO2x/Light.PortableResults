using System;
using System.Collections.Generic;

namespace Light.Results.Metadata;

/// <summary>
/// Internal backing storage for <see cref="MetadataObject" />. Owns parallel arrays of keys and values,
/// sorted by key for deterministic ordering.
/// </summary>
internal sealed class MetadataObjectData
{
    private const int DictionaryThreshold = 8;

    private readonly string[] _keys;
    private readonly MetadataValue[] _values;
    private Dictionary<string, int>? _indexLookup;

    internal MetadataObjectData(string[] keys, MetadataValue[] values)
    {
        if (keys is null)
        {
            throw new ArgumentNullException(nameof(keys));
        }

        if (values is null)
        {
            throw new ArgumentNullException(nameof(values));
        }

        if (keys.Length != values.Length)
        {
            throw new ArgumentException("Keys and values must have the same length.");
        }

        _keys = keys;
        _values = values;
    }

    public static MetadataObjectData Empty { get; } = new ([], []);

    public int Count => _keys.Length;

    public ReadOnlySpan<string> Keys => _keys;
    public ReadOnlySpan<MetadataValue> Values => _values;

    public string GetKey(int index)
    {
        return (uint) index >= (uint) _keys.Length ?
            throw new ArgumentOutOfRangeException(nameof(index)) :
            _keys[index];
    }

    public MetadataValue GetValue(int index) =>
        (uint) index >= (uint) _values.Length ?
            throw new ArgumentOutOfRangeException(nameof(index)) :
            _values[index];

    public bool TryGetValue(string key, out MetadataValue value)
    {
        if (key is null)
        {
            throw new ArgumentNullException(nameof(key));
        }

        var index = FindIndex(key);
        if (index >= 0)
        {
            value = _values[index];
            return true;
        }

        value = default;
        return false;
    }

    private int FindIndex(string key)
    {
        return _keys.Length switch
        {
            0 => -1,
            > DictionaryThreshold => FindIndexWithDictionary(key),
            _ => FindIndexLinear(key)
        };
    }

    private int FindIndexLinear(string key)
    {
        for (var i = 0; i < _keys.Length; i++)
        {
            if (string.Equals(_keys[i], key, StringComparison.Ordinal))
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
            var dict = new Dictionary<string, int>(_keys.Length, StringComparer.Ordinal);
            for (var i = 0; i < _keys.Length; i++)
            {
                dict[_keys[i]] = i;
            }

            _indexLookup = dict;
        }

        return _indexLookup.TryGetValue(key, out var index) ? index : -1;
    }

    public string[] GetKeys() => _keys;
    public MetadataValue[] GetValues() => _values;
}
