using System;
using System.Collections.Generic;
using System.Globalization;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Engines;
using Light.PortableResults.Numbers;

namespace Benchmarks;

[MemoryDiagnoser]
[GroupBenchmarksBy(BenchmarkDotNet.Configs.BenchmarkLogicalGroupRule.ByCategory)]
[CategoriesColumn]
public class CanonicalFloatingPointFormatterBenchmarks
{
    private const int ValueCount = 1_024;

    private readonly Consumer _consumer = new();
    private double[] _doubleValues = null!;
    private float[] _singleValues = null!;

    public IEnumerable<string> Workloads => ["CommonShort", "RandomFinite"];

    [ParamsSource(nameof(Workloads))]
    public string Workload { get; set; } = null!;

    [GlobalSetup]
    public void Setup()
    {
        _doubleValues = new double[ValueCount];
        _singleValues = new float[ValueCount];

        if (Workload == "CommonShort")
        {
            double[] doubles = [0.0, -0.0, 0.1, -1.5, 42.0, 1e-5, 1e16, Math.PI];
            float[] singles = [0.0f, -0.0f, 0.1f, -1.5f, 42.0f, 1e-5f, 1e8f, MathF.PI];
            for (var index = 0; index < ValueCount; index++)
            {
                _doubleValues[index] = doubles[index % doubles.Length];
                _singleValues[index] = singles[index % singles.Length];
            }

            return;
        }

        var random = new Random(0x5EED_0058);
        var bytes = new byte[sizeof(ulong)];
        for (var index = 0; index < ValueCount;)
        {
            random.NextBytes(bytes);
            var bits = BitConverter.ToUInt64(bytes, 0);
            if ((bits & 0x7FF0000000000000UL) != 0x7FF0000000000000UL)
            {
                _doubleValues[index++] = BitConverter.Int64BitsToDouble((long) bits);
            }
        }

        for (var index = 0; index < ValueCount;)
        {
            random.NextBytes(bytes);
            var bits = BitConverter.ToUInt32(bytes, 0);
            if ((bits & 0x7F800000U) != 0x7F800000U)
            {
                _singleValues[index++] = BitConverter.Int32BitsToSingle((int) bits);
            }
        }
    }

    [Benchmark(Baseline = true)]
    [BenchmarkCategory("DoubleTryFormat")]
    public int RuntimeDoubleTryFormat()
    {
        Span<char> destination = stackalloc char[32];
        var totalLength = 0;
        foreach (var value in _doubleValues)
        {
            totalLength += RuntimeTryFormat(value, destination);
        }

        return totalLength;
    }

    [Benchmark]
    [BenchmarkCategory("DoubleTryFormat")]
    public int CanonicalDoubleTryFormat()
    {
        Span<char> destination = stackalloc char[32];
        var totalLength = 0;
        foreach (var value in _doubleValues)
        {
            CanonicalFloatingPointFormatter.TryFormat(value, destination, out var charsWritten);
            totalLength += charsWritten;
        }

        return totalLength;
    }

    [Benchmark(Baseline = true)]
    [BenchmarkCategory("SingleTryFormat")]
    public int RuntimeSingleTryFormat()
    {
        Span<char> destination = stackalloc char[24];
        var totalLength = 0;
        foreach (var value in _singleValues)
        {
            totalLength += RuntimeTryFormat(value, destination);
        }

        return totalLength;
    }

    [Benchmark]
    [BenchmarkCategory("SingleTryFormat")]
    public int CanonicalSingleTryFormat()
    {
        Span<char> destination = stackalloc char[24];
        var totalLength = 0;
        foreach (var value in _singleValues)
        {
            CanonicalFloatingPointFormatter.TryFormat(value, destination, out var charsWritten);
            totalLength += charsWritten;
        }

        return totalLength;
    }

    [Benchmark(Baseline = true)]
    [BenchmarkCategory("DoubleFormat")]
    public void RuntimeDoubleFormat()
    {
        Span<char> destination = stackalloc char[32];
        foreach (var value in _doubleValues)
        {
            var charsWritten = RuntimeTryFormat(value, destination);
            _consumer.Consume(new string(destination[..charsWritten]));
        }
    }

    [Benchmark]
    [BenchmarkCategory("DoubleFormat")]
    public void CanonicalDoubleFormat()
    {
        foreach (var value in _doubleValues)
        {
            _consumer.Consume(CanonicalFloatingPointFormatter.Format(value));
        }
    }

    [Benchmark(Baseline = true)]
    [BenchmarkCategory("SingleFormat")]
    public void RuntimeSingleFormat()
    {
        Span<char> destination = stackalloc char[24];
        foreach (var value in _singleValues)
        {
            var charsWritten = RuntimeTryFormat(value, destination);
            _consumer.Consume(new string(destination[..charsWritten]));
        }
    }

    [Benchmark]
    [BenchmarkCategory("SingleFormat")]
    public void CanonicalSingleFormat()
    {
        foreach (var value in _singleValues)
        {
            _consumer.Consume(CanonicalFloatingPointFormatter.Format(value));
        }
    }

    private static int RuntimeTryFormat(double value, Span<char> destination)
    {
        value.TryFormat(destination, out var charsWritten, "R", CultureInfo.InvariantCulture);
        return AppendMarker(destination, charsWritten);
    }

    private static int RuntimeTryFormat(float value, Span<char> destination)
    {
        value.TryFormat(destination, out var charsWritten, "R", CultureInfo.InvariantCulture);
        return AppendMarker(destination, charsWritten);
    }

    private static int AppendMarker(Span<char> destination, int charsWritten)
    {
        var text = destination[..charsWritten];
        if (text.IndexOf('.') >= 0 || text.IndexOf('E') >= 0 || text.IndexOf('e') >= 0)
        {
            return charsWritten;
        }

        destination[charsWritten] = '.';
        destination[charsWritten + 1] = '0';
        return charsWritten + 2;
    }
}
