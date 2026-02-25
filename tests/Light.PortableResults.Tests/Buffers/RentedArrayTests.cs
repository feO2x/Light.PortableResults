using System;
using System.Buffers;
using System.Collections.Generic;
using FluentAssertions;
using Light.PortableResults.Buffers;
using Xunit;

namespace Light.PortableResults.Tests.Buffers;

public sealed class RentedArrayTests
{
    [Fact]
    public void Memory_ShouldReturnRequestedLength()
    {
        var pool = new TrackingByteArrayPool();
        using var pooledArray = CreateRentedArrayWithLength(pool, 3, 1, 2, 3, 4, 5);

        var memory = pooledArray.Memory;

        memory.ToArray().Should().Equal(1, 2, 3);
    }

    [Fact]
    public void Span_ShouldReturnRequestedLength()
    {
        var pool = new TrackingByteArrayPool();
        using var pooledArray = CreatePooledArray(pool, 1, 2, 3, 4, 5);

        var span = pooledArray.Span;

        span.ToArray().Should().Equal(1, 2, 3, 4, 5);
    }

    [Fact]
    public void Dispose_ShouldReturnArrayToPoolOnlyOnce()
    {
        var pool = new TrackingByteArrayPool();
        var pooledArray = CreatePooledArray(pool, 10, 20, 30);

        pooledArray.Dispose();
        pooledArray.Dispose();

        pool.ReturnCalls.Should().Be(1);
        pool.ReturnedArrays.Should().ContainSingle();
    }

    [Fact]
    public void Dispose_WhenCalledMultipleTimes_ShouldReturnArrayToPoolOnlyOnce()
    {
        var pool = new TrackingByteArrayPool();
        var pooledArray = CreatePooledArray(pool, 10, 20, 30);
        var copied = pooledArray;

        pooledArray.Dispose();
        copied.Dispose();

        pool.ReturnCalls.Should().Be(1);
        pool.ReturnedArrays.Should().ContainSingle();
    }

    [Fact]
    public void Memory_AfterDispose_ShouldThrowInvalidOperationException()
    {
        var pool = new TrackingByteArrayPool();
        var pooledArray = CreatePooledArray(pool, 1, 2);
        pooledArray.Dispose();

        var act = () => _ = pooledArray.Memory;

        act.Should().Throw<InvalidOperationException>()
           .WithMessage("*already disposed*");
    }

    private static IRentedArray CreatePooledArray(ArrayPool<byte> pool, params byte[] bytes) =>
        CreateRentedArrayWithLength(pool, bytes.Length, bytes);

    private static IRentedArray CreateRentedArrayWithLength(ArrayPool<byte> pool, int length, params byte[] bytes)
    {
        var writer = new RentedArrayBufferWriter(pool, bytes.Length);
        bytes.CopyTo(writer.GetSpan(bytes.Length));
        writer.Advance(length);
        return writer.FinishWriting();
    }

    private sealed class TrackingByteArrayPool : ArrayPool<byte>
    {
        public int ReturnCalls { get; private set; }

        public List<byte[]> ReturnedArrays { get; } = new ();

        public override byte[] Rent(int minimumLength) => new byte[Math.Max(minimumLength, 1)];

        public override void Return(byte[] array, bool clearArray = false)
        {
            ReturnCalls++;
            ReturnedArrays.Add(array);
        }
    }
}
