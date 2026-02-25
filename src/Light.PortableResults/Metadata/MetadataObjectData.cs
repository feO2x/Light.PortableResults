using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace Light.PortableResults.Metadata;

/// <summary>
/// Internal backing storage for <see cref="MetadataObject" />. Owns a single array of entries
/// stored in insertion order.
/// </summary>
internal sealed class MetadataObjectData : IEquatable<MetadataObjectData>
{
    private const int DictionaryThreshold = 10;

    private readonly KeyValuePair<string, MetadataValue>[] _entries;
    private readonly IEqualityComparer<string>? _keyComparer;
    private string? _cachedStringRepresentation;
    private bool? _hasOnlyPrimitiveChildren;
    private Dictionary<string, int>? _indexLookup;

    internal MetadataObjectData(KeyValuePair<string, MetadataValue>[] entries, IEqualityComparer<string>? keyComparer)
    {
        _entries = entries ?? throw new ArgumentNullException(nameof(entries));
        _keyComparer = keyComparer;
    }

    public static MetadataObjectData Empty { get; } = new ([], null);

    public int Count => _entries.Length;

    public bool HasOnlyPrimitiveChildren
    {
        get
        {
            if (_hasOnlyPrimitiveChildren.HasValue)
            {
                return _hasOnlyPrimitiveChildren.Value;
            }

            foreach (var kvp in _entries)
            {
                if (!kvp.Value.Kind.IsPrimitive())
                {
                    _hasOnlyPrimitiveChildren = false;
                    return false;
                }
            }

            _hasOnlyPrimitiveChildren = true;
            return true;
        }
    }

    public bool Equals(MetadataObjectData? other)
    {
        if (other is null)
        {
            return false;
        }

        if (Count != other.Count)
        {
            return false;
        }

        return _keyComparer is not null ?
            CompareWithKeyComparer(this, other, _keyComparer) :
            CompareWithoutKeyComparer(this, other);
    }

    private static bool CompareWithoutKeyComparer(MetadataObjectData x, MetadataObjectData y)
    {
        Debug.Assert(x.Count == y.Count);

        for (var i = 0; i < x._entries.Length; i++)
        {
            var xEntry = x._entries[i];
            var yEntry = y._entries[i];

            if (!string.Equals(xEntry.Key, yEntry.Key, StringComparison.Ordinal) ||
                !xEntry.Value.Equals(yEntry.Value))
            {
                return false;
            }
        }

        return true;
    }

    private static bool CompareWithKeyComparer(
        MetadataObjectData x,
        MetadataObjectData y,
        IEqualityComparer<string> keyComparer
    )
    {
        Debug.Assert(x.Count == y.Count);

        for (var i = 0; i < x._entries.Length; i++)
        {
            var xEntry = x._entries[i];
            var yEntry = y._entries[i];

            if (!keyComparer.Equals(xEntry.Key, yEntry.Key) ||
                !xEntry.Value.Equals(yEntry.Value))
            {
                return false;
            }
        }

        return true;
    }

    public ref readonly KeyValuePair<string, MetadataValue> GetEntry(int index) => ref _entries[index];

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

    private int FindIndex(string key) =>
        _entries.Length switch
        {
            0 => -1,
            > DictionaryThreshold => FindIndexWithDictionary(key),
            _ => FindIndexLinear(key)
        };

    private int FindIndexLinear(string key)
    {
        return _keyComparer is null ?
            FindIndexLinearWithoutKeyComparer(_entries, key) :
            FindIndexLinearWithKeyComparer(_entries, key, _keyComparer);

        static int FindIndexLinearWithoutKeyComparer(KeyValuePair<string, MetadataValue>[] entries, string key)
        {
            for (var i = 0; i < entries.Length; i++)
            {
                if (key.Equals(entries[i].Key, StringComparison.Ordinal))
                {
                    return i;
                }
            }

            return -1;
        }

        static int FindIndexLinearWithKeyComparer(
            KeyValuePair<string, MetadataValue>[] entries,
            string key,
            IEqualityComparer<string> comparer
        )
        {
            for (var i = 0; i < entries.Length; i++)
            {
                if (comparer.Equals(key, entries[i].Key))
                {
                    return i;
                }
            }

            return -1;
        }
    }

    private int FindIndexWithDictionary(string key)
    {
        if (_indexLookup is null)
        {
            var dictionary = _keyComparer is null ?
                new Dictionary<string, int>(_entries.Length) :
                new Dictionary<string, int>(_entries.Length, _keyComparer);
            for (var i = 0; i < _entries.Length; i++)
            {
                dictionary[_entries[i].Key] = i;
            }

            _indexLookup = dictionary;
        }

        return _indexLookup.TryGetValue(key, out var index) ? index : -1;
    }

    public KeyValuePair<string, MetadataValue>[] GetEntries() => _entries;

    public override bool Equals(object? obj) => Equals(obj as MetadataObjectData);

    public override int GetHashCode()
    {
        var hash = new HashCode();
        for (var i = 0; i < _entries.Length; i++)
        {
            hash.Add(_entries[i].Key);
            hash.Add(_entries[i].Value);
        }

        return hash.ToHashCode();
    }

    public override string ToString() => _cachedStringRepresentation ??= CreateStringRepresentation(_entries);

    private static string CreateStringRepresentation(KeyValuePair<string, MetadataValue>[] entries)
    {
        switch (entries.Length)
        {
            case 0: return MetadataObject.EmptyObjectStringRepresentation;
            case 1:
            {
                var kvp = entries[0];
                return $"{{\"{kvp.Key}\": {kvp.Value.ToString()}}}";
            }
            case 2:
            {
                var kvp1 = entries[0];
                var kvp2 = entries[1];
                return $"{{\"{kvp1.Key}\": {kvp1.Value.ToString()}, \"{kvp2.Key}\": {kvp2.Value.ToString()}}}";
            }
            case 3:
            {
                var kvp1 = entries[0];
                var kvp2 = entries[1];
                var kvp3 = entries[2];
                return
                    $"{{\"{kvp1.Key}\": {kvp1.Value.ToString()}, \"{kvp2.Key}\": {kvp2.Value.ToString()}, \"{kvp3.Key}\": {kvp3.Value.ToString()}}}";
            }
            case 4:
            {
                var kvp1 = entries[0];
                var kvp2 = entries[1];
                var kvp3 = entries[2];
                var kvp4 = entries[3];
                return
                    $"{{\"{kvp1.Key}\": {kvp1.Value.ToString()}, \"{kvp2.Key}\": {kvp2.Value.ToString()}, \"{kvp3.Key}\": {kvp3.Value.ToString()}, \"{kvp4.Key}\": {kvp4.Value.ToString()}}}";
            }
            case 5:
            {
                var kvp1 = entries[0];
                var kvp2 = entries[1];
                var kvp3 = entries[2];
                var kvp4 = entries[3];
                var kvp5 = entries[4];
                return
                    $"{{\"{kvp1.Key}\": {kvp1.Value.ToString()}, \"{kvp2.Key}\": {kvp2.Value.ToString()}, \"{kvp3.Key}\": {kvp3.Value.ToString()}, \"{kvp4.Key}\": {kvp4.Value.ToString()}, \"{kvp5.Key}\": {kvp5.Value.ToString()}}}";
            }
            default:
                var builder = new StringBuilder();
                builder.Append('{');
                for (var i = 0; i < entries.Length; i++)
                {
                    if (i > 0)
                    {
                        builder.Append(", ");
                    }

                    var kvp = entries[i];
                    builder.Append('"');
                    builder.Append(kvp.Key);
                    builder.Append("\": ");
                    builder.Append(kvp.Value.ToString());
                }

                builder.Append('}');
                return builder.ToString();
        }
    }
}
