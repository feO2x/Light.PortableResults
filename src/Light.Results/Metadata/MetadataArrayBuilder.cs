using System;
using System.Buffers;

namespace Light.Results.Metadata;

/// <summary>
/// Builder for creating <see cref="MetadataArray" /> instances efficiently using pooled buffers.
/// </summary>
public struct MetadataArrayBuilder : IDisposable
{
    private const int DefaultCapacity = 4;

    private MetadataValue[]? _buffer;
    private bool _built;

    public int Count { get; private set; }

    public static MetadataArrayBuilder Create(int capacity = DefaultCapacity)
    {
        var builder = new MetadataArrayBuilder
        {
            _buffer = ArrayPool<MetadataValue>.Shared.Rent(Math.Max(capacity, DefaultCapacity)),
            Count = 0,
            _built = false
        };
        return builder;
    }

    public void Add(MetadataValue value)
    {
        ThrowIfBuilt();
        EnsureCapacity(Count + 1);
        _buffer![Count++] = value;
    }

    public void AddRange(ReadOnlySpan<MetadataValue> values)
    {
        ThrowIfBuilt();
        EnsureCapacity(Count + values.Length);
        values.CopyTo(_buffer.AsSpan(Count));
        Count += values.Length;
    }

    public MetadataArray Build()
    {
        ThrowIfBuilt();
        _built = true;

        if (Count == 0)
        {
            ReturnBuffer();
            return MetadataArray.Empty;
        }

        var result = new MetadataValue[Count];
        Array.Copy(_buffer!, result, Count);
        ReturnBuffer();

        return new MetadataArray(new MetadataArrayData(result));
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
        if (_buffer is null || _buffer.Length >= required)
        {
            if (_buffer is null)
            {
                _buffer = ArrayPool<MetadataValue>.Shared.Rent(Math.Max(required, DefaultCapacity));
            }

            return;
        }

        var newCapacity = Math.Max(_buffer.Length * 2, required);
        var newBuffer = ArrayPool<MetadataValue>.Shared.Rent(newCapacity);
        Array.Copy(_buffer, newBuffer, Count);
        ArrayPool<MetadataValue>.Shared.Return(_buffer, clearArray: true);
        _buffer = newBuffer;
    }

    private void ReturnBuffer()
    {
        if (_buffer is not null)
        {
            ArrayPool<MetadataValue>.Shared.Return(_buffer, clearArray: true);
            _buffer = null;
        }
    }

    private void ThrowIfBuilt()
    {
        if (_built)
        {
            throw new InvalidOperationException("Builder has already been used to build an array.");
        }
    }
}
