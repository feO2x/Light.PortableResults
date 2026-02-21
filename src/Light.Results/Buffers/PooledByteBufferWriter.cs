using System;
using System.Buffers;

namespace Light.Results.Buffers;

/// <summary>
/// A buffer writer that uses ArrayPool for efficient memory management.
/// Provides high-performance writing to byte arrays with automatic resizing and memory pooling.
/// </summary>
public sealed class PooledByteBufferWriter : IBufferWriter<byte>
{
    /// <summary>
    /// Gets the default initial capacity for the buffer, which is 2048 bytes.
    /// </summary>
    public const int DefaultInitialCapacity = 2048;

    private readonly ArrayPool<byte> _arrayPool;
    private byte[] _buffer;
    private int _index;
    private bool _isDone;

    /// <summary>
    /// Initializes a new instance of the <see cref="PooledByteBufferWriter" /> class.
    /// </summary>
    /// <param name="initialCapacity">The initial capacity of the buffer. Defaults to 2048.</param>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="initialCapacity" /> is less than 0.</exception>
    public PooledByteBufferWriter(int initialCapacity = DefaultInitialCapacity) : this(
        ArrayPool<byte>.Shared,
        initialCapacity
    ) { }

    /// <summary>
    /// Initializes a new instance of the <see cref="PooledByteBufferWriter" /> class.
    /// </summary>
    /// <param name="arrayPool">The array pool instance where buffer arrays are rented from and returned to.</param>
    /// <param name="initialCapacity">The initial capacity of the buffer. Defaults to 2048.</param>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="initialCapacity" /> is less than 0.</exception>
    public PooledByteBufferWriter(ArrayPool<byte> arrayPool, int initialCapacity = DefaultInitialCapacity)
    {
        if (initialCapacity < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(initialCapacity));
        }

        _arrayPool = arrayPool ?? throw new ArgumentNullException(nameof(arrayPool));

        _buffer = arrayPool.Rent(initialCapacity);
        _index = 0;
    }

    /// <summary>
    /// Advances the writer by the specified number of bytes.
    /// </summary>
    /// <param name="count">The number of bytes to advance.</param>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when count is negative.</exception>
    /// <exception cref="InvalidOperationException">Thrown when advancing past the end of the buffer, or when ToPooledArray has already been called.</exception>
    public void Advance(int count)
    {
        EnsureNotDone();
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

    /// <summary>
    /// Gets a Memory&lt;byte> that can be written to.
    /// </summary>
    /// <param name="sizeHint">The minimum size of the memory to return. Defaults to 0.</param>
    /// <returns>A Memory&lt;byte> that can be written to.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="sizeHint" /> is negative.</exception>
    /// <exception cref="InvalidOperationException">Thrown when ToPooledArray has already been called.</exception>
    public Memory<byte> GetMemory(int sizeHint = 0)
    {
        EnsureNotDone();
        CheckAndResizeBuffer(sizeHint);
        return _buffer.AsMemory(_index);
    }

    /// <summary>
    /// Gets a Span&lt;byte> that can be written to.
    /// </summary>
    /// <param name="sizeHint">The minimum size of the span to return. Defaults to 0.</param>
    /// <returns>A Span&lt;byte> that can be written to.</returns>
    /// <exception cref="InvalidOperationException">Thrown when ToPooledArray has already been called.</exception>
    public Span<byte> GetSpan(int sizeHint = 0)
    {
        EnsureNotDone();
        CheckAndResizeBuffer(sizeHint);
        return _buffer.AsSpan(_index);
    }

    /// <summary>
    /// Creates an instance of <see cref="PooledArray" /> from the current state of the writer.
    /// </summary>
    /// <returns>
    /// An instance of <see cref="PooledArray" /> that lets you access the underlying array in a read-only manner and
    /// that lets you return the array to the Array Pool when the struct instance is disposed.
    /// </returns>
    /// <exception cref="InvalidOperationException">Thrown when ToPooledArray has already been called.</exception>
    public PooledArray ToPooledArray()
    {
        EnsureNotDone();
        _isDone = true;
        return new PooledArray(_buffer, _arrayPool, _index);
    }

    private void EnsureNotDone()
    {
        if (_isDone)
        {
            throw new InvalidOperationException(
                $"{nameof(ToPooledArray)} has already been called. You cannot use the PooledByteBufferWriter after calling {nameof(ToPooledArray)}."
            );
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
        var minimumRequiredLength = (long) _index + sizeHint;
        if (minimumRequiredLength > int.MaxValue)
        {
            throw new ArgumentOutOfRangeException(
                nameof(sizeHint),
                "The requested size is too large for a single buffer."
            );
        }

        var growBy = Math.Max(sizeHint, currentLength);
        long newSizeLong;
        checked
        {
            newSizeLong = (long) currentLength + growBy;
        }

        if (newSizeLong < minimumRequiredLength)
        {
            newSizeLong = minimumRequiredLength;
        }

        if (newSizeLong > int.MaxValue)
        {
            newSizeLong = int.MaxValue;
        }

        var newSize = (int) newSizeLong;

        var newBuffer = _arrayPool.Rent(newSize);
        Array.Copy(_buffer, 0, newBuffer, 0, _index);
        _arrayPool.Return(_buffer);
        _buffer = newBuffer;
    }
}
