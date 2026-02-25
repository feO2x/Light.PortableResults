using System;
using System.Buffers;
using System.Collections.Generic;
using FluentAssertions;
using Light.PortableResults.Buffers;
using Xunit;

namespace Light.PortableResults.Tests.Buffers;

public sealed class RentedArrayBufferWriterTests
{
    [Fact]
    public void Constructor_WithNegativeInitialCapacity_ShouldThrowArgumentOutOfRangeException()
    {
        var act = () => _ = new RentedArrayBufferWriter(-1);

        act.Should().Throw<ArgumentOutOfRangeException>()
           .WithParameterName("initialCapacity");
    }

    [Fact]
    public void Constructor_WithNullArrayPool_ShouldThrowArgumentNullException()
    {
        var act = () => _ = new RentedArrayBufferWriter(null!, 1);

        act.Should().Throw<ArgumentNullException>()
           .WithParameterName("arrayPool");
    }

    [Fact]
    public void GetMemory_WithNegativeSizeHint_ShouldThrowArgumentOutOfRangeException()
    {
        var writer = new RentedArrayBufferWriter(initialCapacity: 1);

        var act = () => _ = writer.GetMemory(-1);

        act.Should().Throw<ArgumentOutOfRangeException>()
           .WithParameterName("sizeHint");
    }

    [Fact]
    public void GetSpan_WithNegativeSizeHint_ShouldThrowArgumentOutOfRangeException()
    {
        var writer = new RentedArrayBufferWriter(initialCapacity: 1);

        var act = () => { writer.GetSpan(-1); };

        act.Should().Throw<ArgumentOutOfRangeException>()
           .WithParameterName("sizeHint");
    }

    [Fact]
    public void Advance_WithNegativeCount_ShouldThrowArgumentOutOfRangeException()
    {
        var writer = new RentedArrayBufferWriter(initialCapacity: 4);

        var act = () => writer.Advance(-1);

        act.Should().Throw<ArgumentOutOfRangeException>()
           .WithParameterName("count");
    }

    [Fact]
    public void Advance_PastBufferEnd_ShouldThrowInvalidOperationException()
    {
        var writer = new RentedArrayBufferWriter(new TrackingByteArrayPool(), initialCapacity: 2);
        writer.Advance(2);

        var act = () => writer.Advance(1);

        act.Should().Throw<InvalidOperationException>()
           .WithMessage("*past the end of the buffer*");
    }

    [Fact]
    public void FinishWriting_ShouldReturnWrittenBytes()
    {
        var writer = new RentedArrayBufferWriter(initialCapacity: 4);
        var span = writer.GetSpan(3);
        span[0] = 9;
        span[1] = 8;
        span[2] = 7;
        writer.Advance(3);

        using var pooledArray = writer.FinishWriting();

        pooledArray.Span.ToArray().Should().Equal(9, 8, 7);
    }

    [Fact]
    public void FinishWriting_AfterCalled_ShouldPreventFurtherWritingButAllowAdditionalHandles()
    {
        var writer = new RentedArrayBufferWriter(initialCapacity: 2);
        using var pooledArray1 = writer.FinishWriting();

        var advanceAct = () => writer.Advance(1);
        var getMemoryAct = () => writer.GetMemory();
        var getSpanAct = () => { writer.GetSpan(); };
        var finishWritingAct = () => writer.FinishWriting();

        advanceAct.Should().Throw<InvalidOperationException>().WithMessage("*FinishWriting*already been called*");
        getMemoryAct.Should().Throw<InvalidOperationException>().WithMessage("*FinishWriting*already been called*");
        getSpanAct.Should().Throw<InvalidOperationException>().WithMessage("*FinishWriting*already been called*");

        var pooledArray2 = finishWritingAct.Should().NotThrow().Subject;
        pooledArray2.Span.ToArray().Should().Equal(pooledArray1.Span.ToArray());
        pooledArray2.Dispose();
    }

    [Fact]
    public void FinishWriting_MultipleHandles_ShouldShareIdempotentDisposeState()
    {
        var trackingPool = new TrackingByteArrayPool();
        var writer = new RentedArrayBufferWriter(trackingPool, initialCapacity: 2);
        writer.Advance(1);

        var pooledArray1 = writer.FinishWriting();
        var pooledArray2 = writer.FinishWriting();

        pooledArray1.Dispose();
        pooledArray2.Dispose();

        trackingPool.ReturnedArrays.Should().ContainSingle();
    }

    [Fact]
    public void GetMemory_WithZeroSizeHint_ShouldProvideWritableBuffer()
    {
        var writer = new RentedArrayBufferWriter(initialCapacity: 1);

        var memory = writer.GetMemory();

        memory.Length.Should().BeGreaterThan(0);
    }

    [Fact]
    public void CheckAndResizeBuffer_ShouldReturnOldBufferToPool()
    {
        var trackingPool = new TrackingByteArrayPool();
        var writer = new RentedArrayBufferWriter(trackingPool, initialCapacity: 2);

        var span = writer.GetSpan(2);
        span[0] = 1;
        span[1] = 2;
        writer.Advance(2);

        _ = writer.GetMemory(3);

        trackingPool.ReturnedArrays.Should().ContainSingle();
        trackingPool.ReturnedArrays[0].Length.Should().Be(2);
    }

    [Fact]
    public void FinishWriting_Dispose_ShouldReturnCurrentBufferToPool()
    {
        var trackingPool = new TrackingByteArrayPool();
        var writer = new RentedArrayBufferWriter(trackingPool, initialCapacity: 2);
        writer.Advance(1);

        var pooledArray = writer.FinishWriting();
        pooledArray.Dispose();

        trackingPool.ReturnedArrays.Should().ContainSingle();
    }

    [Fact]
    public void Dispose_WithoutFinishWriting_ShouldReturnCurrentBufferToPoolOnlyOnce()
    {
        var trackingPool = new TrackingByteArrayPool();
        var writer = new RentedArrayBufferWriter(trackingPool, initialCapacity: 2);

        writer.Dispose();
        writer.Dispose();

        trackingPool.ReturnedArrays.Should().ContainSingle();
    }

    [Fact]
    public void Dispose_AfterFinishWriting_ShouldShareIdempotenceWithPooledArrayHandles()
    {
        var trackingPool = new TrackingByteArrayPool();
        var writer = new RentedArrayBufferWriter(trackingPool, initialCapacity: 2);
        writer.Advance(1);
        var pooledArray = writer.FinishWriting();

        writer.Dispose();
        pooledArray.Dispose();

        trackingPool.ReturnedArrays.Should().ContainSingle();
    }

    [Fact]
    public void FinishWriting_AfterWriterDispose_ShouldThrowInvalidOperationException()
    {
        var writer = new RentedArrayBufferWriter(initialCapacity: 2);
        writer.Dispose();

        var act = () => writer.FinishWriting();

        act.Should().Throw<InvalidOperationException>()
           .WithMessage("*already disposed*");
    }

    [Fact]
    public void GetMemory_WithVeryLargeSizeHint_ShouldNeverRentNegativeLength()
    {
        var pool = new OverflowAwareArrayPool(initialBufferLength: 2, resizedBufferLength: 8);
        var writer = new RentedArrayBufferWriter(pool, initialCapacity: 2);

        var act = () => _ = writer.GetMemory(int.MaxValue);

        act.Should().NotThrow();
        pool.LastRentedMinimumLength.Should().Be(int.MaxValue);
        pool.ReturnedArrays.Should().ContainSingle();
    }

    [Fact]
    public void GetMemory_WithTooLargeRequiredLength_ShouldThrowArgumentOutOfRangeException()
    {
        var writer = new RentedArrayBufferWriter(new TrackingByteArrayPool(), initialCapacity: 2);
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
