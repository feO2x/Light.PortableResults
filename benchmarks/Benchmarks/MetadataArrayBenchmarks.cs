using System.Diagnostics.CodeAnalysis;
using BenchmarkDotNet.Attributes;
using Light.Results.Metadata;

namespace Benchmarks;

/// <summary>
/// Benchmarks for MetadataArray vs object[] with boxing.
/// </summary>
[MemoryDiagnoser]
[SimpleJob]
[SuppressMessage("Performance", "CA1822:Mark members as static")] // benchmark methods must not be static
public class MetadataArrayBenchmarks
{
    private MetadataArray _metadataArray;
    private object?[] _objectArray = null!;

    [GlobalSetup]
    public void Setup()
    {
        _metadataArray = MetadataArray.Create(1L, 2L, 3L, 4L, 5L);
        _objectArray = [1L, 2L, 3L, 4L, 5L];
    }

    [Benchmark(Baseline = true)]
    public object?[] CreateObjectArray()
    {
        return [1L, 2L, 3L, 4L, 5L];
    }

    [Benchmark]
    public MetadataArray CreateMetadataArray()
    {
        return MetadataArray.Create(1L, 2L, 3L, 4L, 5L);
    }

    [Benchmark]
    public long ObjectArrayAccess()
    {
        return (long) _objectArray[2]!;
    }

    [Benchmark]
    public long MetadataArrayAccess()
    {
        _metadataArray[2].TryGetInt64(out var value);
        return value;
    }

    [Benchmark]
    public int ObjectArrayIterate()
    {
        var sum = 0;
        foreach (var unused in _objectArray)
        {
            sum++;
        }

        return sum;
    }

    [Benchmark]
    public int MetadataArrayIterate()
    {
        var sum = 0;
        foreach (var unused in _metadataArray)
        {
            sum++;
        }

        return sum;
    }
}
