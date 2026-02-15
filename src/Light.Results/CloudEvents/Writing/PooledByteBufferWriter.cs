using System;
using System.Buffers;

namespace Light.Results.CloudEvents.Writing;

internal sealed class PooledByteBufferWriter : IBufferWriter<byte>, IDisposable
{
    private const int DefaultInitialCapacity = 2048;
    private byte[] _buffer;
    private int _index;

    public PooledByteBufferWriter(int initialCapacity = DefaultInitialCapacity)
    {
        _buffer = ArrayPool<byte>.Shared.Rent(initialCapacity);
        _index = 0;
    }

    public ReadOnlyMemory<byte> WrittenMemory => _buffer.AsMemory(0, _index);

    public ReadOnlySpan<byte> WrittenSpan => _buffer.AsSpan(0, _index);

    public int WrittenCount => _index;

    public void Advance(int count)
    {
        if (count < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(count));
        }

        if (_index > _buffer.Length - count)
        {
            throw new InvalidOperationException("Cannot advance past the end of the buffer.");
        }

        _index += count;
    }

    public Memory<byte> GetMemory(int sizeHint = 0)
    {
        CheckAndResizeBuffer(sizeHint);
        return _buffer.AsMemory(_index);
    }

    public Span<byte> GetSpan(int sizeHint = 0)
    {
        CheckAndResizeBuffer(sizeHint);
        return _buffer.AsSpan(_index);
    }

    public byte[] ToArray()
    {
        var result = new byte[_index];
        Array.Copy(_buffer, 0, result, 0, _index);
        return result;
    }

    public void Dispose()
    {
        var toReturn = _buffer;
        _buffer = Array.Empty<byte>();
        _index = 0;
        if (toReturn.Length > 0)
        {
            ArrayPool<byte>.Shared.Return(toReturn);
        }
    }

    private void CheckAndResizeBuffer(int sizeHint)
    {
        if (sizeHint < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(sizeHint));
        }

        if (sizeHint == 0)
        {
            sizeHint = 1;
        }

        if (sizeHint <= _buffer.Length - _index)
        {
            return;
        }

        var currentLength = _buffer.Length;
        var growBy = Math.Max(sizeHint, currentLength);
        var newSize = currentLength + growBy;

        if ((uint) newSize > int.MaxValue)
        {
            newSize = currentLength + sizeHint;
        }

        var newBuffer = ArrayPool<byte>.Shared.Rent(newSize);
        Array.Copy(_buffer, 0, newBuffer, 0, _index);
        ArrayPool<byte>.Shared.Return(_buffer);
        _buffer = newBuffer;
    }
}
