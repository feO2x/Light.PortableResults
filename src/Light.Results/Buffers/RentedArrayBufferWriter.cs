using System;
using System.Buffers;

namespace Light.Results.Buffers;

/// <summary>
/// A buffer writer that uses ArrayPool&lt;byte> instead of instantiating new arrays for each write operation.
/// Provides high-performance writing to byte arrays with automatic resizing and memory pooling.
/// </summary>
public sealed class RentedArrayBufferWriter : IBufferWriter<byte>, IRentedArray
{
    /// <summary>
    /// Gets the default initial capacity for the buffer, which is 2048 bytes.
    /// </summary>
    public const int DefaultInitialCapacity = 2048;

    private readonly ArrayPool<byte> _arrayPool;
    private byte[] _buffer;
    private int _index;
    private WriterState _state;

    /// <summary>
    /// Initializes a new instance of the <see cref="RentedArrayBufferWriter" /> class.
    /// </summary>
    /// <param name="initialCapacity">The initial capacity of the buffer. Defaults to 2048.</param>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="initialCapacity" /> is less than 0.</exception>
    public RentedArrayBufferWriter(int initialCapacity = DefaultInitialCapacity) : this(
        ArrayPool<byte>.Shared,
        initialCapacity
    ) { }

    /// <summary>
    /// Initializes a new instance of the <see cref="RentedArrayBufferWriter" /> class.
    /// </summary>
    /// <param name="arrayPool">The array pool instance where buffer arrays are rented from and returned to.</param>
    /// <param name="initialCapacity">The initial capacity of the buffer. Defaults to 2048.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="arrayPool" /> is <see langword="null" />.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="initialCapacity" /> is less than 0.</exception>
    public RentedArrayBufferWriter(ArrayPool<byte> arrayPool, int initialCapacity = DefaultInitialCapacity)
    {
        if (initialCapacity < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(initialCapacity));
        }

        _arrayPool = arrayPool ?? throw new ArgumentNullException(nameof(arrayPool));

        _buffer = arrayPool.Rent(initialCapacity);
        _index = 0;
        _state = WriterState.Writable;
    }

    /// <summary>
    /// Advances the writer by the specified number of bytes.
    /// </summary>
    /// <param name="count">The number of bytes to advance.</param>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when count is negative.</exception>
    /// <exception cref="InvalidOperationException">
    /// Thrown when advancing past the end of the buffer, or when FinishWriting has already been called.
    /// </exception>
    public void Advance(int count)
    {
        EnsureWritable();
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
    /// <exception cref="InvalidOperationException">Thrown when FinishWriting has already been called.</exception>
    public Memory<byte> GetMemory(int sizeHint = 0)
    {
        EnsureWritable();
        CheckAndResizeBuffer(sizeHint);
        return _buffer.AsMemory(_index);
    }

    /// <summary>
    /// Gets a Span&lt;byte> that can be written to.
    /// </summary>
    /// <param name="sizeHint">The minimum size of the span to return. Defaults to 0.</param>
    /// <returns>A Span&lt;byte> that can be written to.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="sizeHint" /> is negative.</exception>
    /// <exception cref="InvalidOperationException">Thrown when FinishWriting has already been called.</exception>
    public Span<byte> GetSpan(int sizeHint = 0)
    {
        EnsureWritable();
        CheckAndResizeBuffer(sizeHint);
        return _buffer.AsSpan(_index);
    }

    /// <summary>
    /// Returns the currently rented buffer to the array pool.
    /// </summary>
    /// <remarks>
    /// This method is idempotent and can be called multiple times safely.
    /// </remarks>
    public void Dispose()
    {
        if (_state == WriterState.Disposed)
        {
            return;
        }

        _arrayPool.Return(_buffer);
        _buffer = [];
        _state = WriterState.Disposed;
    }

    /// <inheritdoc />
    public ReadOnlyMemory<byte> Memory => GetRentedArraySegmentAsMemory();

    /// <inheritdoc />
    public ReadOnlySpan<byte> Span => Memory.Span;

    /// <summary>
    /// Finishes writing to the buffer and returns an instance of <see cref="IRentedArray" /> that lets you access the
    /// underlying array in a read-only manner and that lets you return the array to the Array Pool on disposal.
    /// </summary>
    /// <returns>
    /// An instance of <see cref="IRentedArray" /> that lets you access the underlying array in a read-only manner and
    /// that lets you return the array to the Array Pool on disposal.
    /// </returns>
    /// <exception cref="InvalidOperationException">Thrown when the buffer writer is already disposed.</exception>
    public IRentedArray FinishWriting()
    {
        if (_state == WriterState.Disposed)
        {
            throw new InvalidOperationException("The buffer writer is already disposed");
        }

        if (_state == WriterState.Writable)
        {
            _state = WriterState.Leased;
        }

        return this;
    }

    private ReadOnlyMemory<byte> GetRentedArraySegmentAsMemory()
    {
        if (_state == WriterState.Disposed)
        {
            throw new InvalidOperationException("The rented array is already disposed");
        }

        return _buffer.AsMemory(0, _index);
    }

    private void EnsureWritable()
    {
        if (_state != WriterState.Writable)
        {
            throw new InvalidOperationException(
                $"{nameof(FinishWriting)} has already been called. You cannot use the buffer writer after calling {nameof(FinishWriting)}."
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

    private enum WriterState
    {
        Writable,
        Leased,
        Disposed
    }
}
