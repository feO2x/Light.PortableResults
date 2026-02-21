using System;
using System.Buffers;
using System.Collections.Generic;
using FluentAssertions;
using Light.Results.Buffers;
using Xunit;

namespace Light.Results.Tests.Buffers;

public sealed class PooledByteBufferWriterTests
{
    [Fact]
    public void Constructor_WithNegativeInitialCapacity_ShouldThrowArgumentOutOfRangeException()
    {
        var act = () => _ = new PooledByteBufferWriter(-1);

        act.Should().Throw<ArgumentOutOfRangeException>()
           .WithParameterName("initialCapacity");
    }

    [Fact]
    public void Constructor_WithNullArrayPool_ShouldThrowArgumentNullException()
    {
        var act = () => _ = new PooledByteBufferWriter(null!, 1);

        act.Should().Throw<ArgumentNullException>()
           .WithParameterName("arrayPool");
    }

    [Fact]
    public void GetMemory_WithNegativeSizeHint_ShouldThrowArgumentOutOfRangeException()
    {
        var writer = new PooledByteBufferWriter(initialCapacity: 1);

        var act = () => _ = writer.GetMemory(-1);

        act.Should().Throw<ArgumentOutOfRangeException>()
           .WithParameterName("sizeHint");
    }

    [Fact]
    public void GetSpan_WithNegativeSizeHint_ShouldThrowArgumentOutOfRangeException()
    {
        var writer = new PooledByteBufferWriter(initialCapacity: 1);

        var act = () => { writer.GetSpan(-1); };

        act.Should().Throw<ArgumentOutOfRangeException>()
           .WithParameterName("sizeHint");
    }

    [Fact]
    public void Advance_WithNegativeCount_ShouldThrowArgumentOutOfRangeException()
    {
        var writer = new PooledByteBufferWriter(initialCapacity: 4);

        var act = () => writer.Advance(-1);

        act.Should().Throw<ArgumentOutOfRangeException>()
           .WithParameterName("count");
    }

    [Fact]
    public void Advance_PastBufferEnd_ShouldThrowInvalidOperationException()
    {
        var writer = new PooledByteBufferWriter(new TrackingByteArrayPool(), initialCapacity: 2);
        writer.Advance(2);

        var act = () => writer.Advance(1);

        act.Should().Throw<InvalidOperationException>()
           .WithMessage("*past the end of the buffer*");
    }

    [Fact]
    public void ToPooledArray_ShouldReturnWrittenBytes()
    {
        var writer = new PooledByteBufferWriter(initialCapacity: 4);
        var span = writer.GetSpan(3);
        span[0] = 9;
        span[1] = 8;
        span[2] = 7;
        writer.Advance(3);

        using var pooledArray = writer.ToPooledArray();

        pooledArray.AsSpan().ToArray().Should().Equal(9, 8, 7);
    }

    [Fact]
    public void ToPooledArray_AfterCalled_ShouldPreventFurtherUsage()
    {
        var writer = new PooledByteBufferWriter(initialCapacity: 2);
        using var pooledArray = writer.ToPooledArray();

        var advanceAct = () => writer.Advance(1);
        var getMemoryAct = () => writer.GetMemory();
        var getSpanAct = () => { writer.GetSpan(); };
        var toPooledArrayAct = () => writer.ToPooledArray();

        advanceAct.Should().Throw<InvalidOperationException>().WithMessage("*ToPooledArray*already been called*");
        getMemoryAct.Should().Throw<InvalidOperationException>().WithMessage("*ToPooledArray*already been called*");
        getSpanAct.Should().Throw<InvalidOperationException>().WithMessage("*ToPooledArray*already been called*");
        toPooledArrayAct.Should().Throw<InvalidOperationException>().WithMessage("*ToPooledArray*already been called*");
    }

    [Fact]
    public void GetMemory_WithZeroSizeHint_ShouldProvideWritableBuffer()
    {
        var writer = new PooledByteBufferWriter(initialCapacity: 1);

        var memory = writer.GetMemory();

        memory.Length.Should().BeGreaterThan(0);
    }

    [Fact]
    public void CheckAndResizeBuffer_ShouldReturnOldBufferToPool()
    {
        var trackingPool = new TrackingByteArrayPool();
        var writer = new PooledByteBufferWriter(trackingPool, initialCapacity: 2);

        var span = writer.GetSpan(2);
        span[0] = 1;
        span[1] = 2;
        writer.Advance(2);

        _ = writer.GetMemory(3);

        trackingPool.ReturnedArrays.Should().ContainSingle();
        trackingPool.ReturnedArrays[0].Length.Should().Be(2);
    }

    [Fact]
    public void ToPooledArray_Dispose_ShouldReturnCurrentBufferToPool()
    {
        var trackingPool = new TrackingByteArrayPool();
        var writer = new PooledByteBufferWriter(trackingPool, initialCapacity: 2);
        writer.Advance(1);

        var pooledArray = writer.ToPooledArray();
        pooledArray.Dispose();

        trackingPool.ReturnedArrays.Should().ContainSingle();
    }

    [Fact]
    public void GetMemory_WithVeryLargeSizeHint_ShouldNeverRentNegativeLength()
    {
        var pool = new OverflowAwareArrayPool(initialBufferLength: 2, resizedBufferLength: 8);
        var writer = new PooledByteBufferWriter(pool, initialCapacity: 2);

        var act = () => _ = writer.GetMemory(int.MaxValue);

        act.Should().NotThrow();
        pool.LastRentedMinimumLength.Should().Be(int.MaxValue);
        pool.ReturnedArrays.Should().ContainSingle();
    }

    [Fact]
    public void GetMemory_WithTooLargeRequiredLength_ShouldThrowArgumentOutOfRangeException()
    {
        var writer = new PooledByteBufferWriter(new TrackingByteArrayPool(), initialCapacity: 2);
        writer.Advance(1);

        var act = () => _ = writer.GetMemory(int.MaxValue);

        act.Should().Throw<ArgumentOutOfRangeException>()
           .WithParameterName("sizeHint")
           .WithMessage("*too large*");
    }

    private sealed class TrackingByteArrayPool : ArrayPool<byte>
    {
        public List<byte[]> ReturnedArrays { get; } = new ();

        public override byte[] Rent(int minimumLength) => new byte[Math.Max(minimumLength, 1)];

        public override void Return(byte[] array, bool clearArray = false) => ReturnedArrays.Add(array);
    }

    private sealed class OverflowAwareArrayPool : ArrayPool<byte>
    {
        private readonly int _initialBufferLength;
        private readonly int _resizedBufferLength;
        private bool _isFirstRent = true;

        public OverflowAwareArrayPool(int initialBufferLength, int resizedBufferLength)
        {
            _initialBufferLength = initialBufferLength;
            _resizedBufferLength = resizedBufferLength;
        }

        public int LastRentedMinimumLength { get; private set; }

        public List<byte[]> ReturnedArrays { get; } = new ();

        public override byte[] Rent(int minimumLength)
        {
            if (minimumLength < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(minimumLength));
            }

            LastRentedMinimumLength = minimumLength;
            if (_isFirstRent)
            {
                _isFirstRent = false;
                return new byte[_initialBufferLength];
            }

            return new byte[_resizedBufferLength];
        }

        public override void Return(byte[] array, bool clearArray = false) => ReturnedArrays.Add(array);
    }
}
