using System.Diagnostics.CodeAnalysis;
using BenchmarkDotNet.Attributes;
using Light.PortableResults.Metadata;

namespace Benchmarks;

/// <summary>
/// Benchmarks for MetadataValue creation and access patterns.
/// Demonstrates zero-allocation for primitive types.
/// </summary>
[MemoryDiagnoser]
[SimpleJob]
[SuppressMessage("Performance", "CA1822:Mark members as static")] // benchmark methods must not be static
public class MetadataValueBenchmarks
{
    [Benchmark]
    public MetadataValue CreateInt64Value()
    {
        return MetadataValue.FromInt64(42);
    }

    [Benchmark]
    public object CreateBoxedInt64()
    {
        return 42L;
    }

    [Benchmark]
    public MetadataValue CreateDoubleValue()
    {
        return MetadataValue.FromDouble(3.14159);
    }

    [Benchmark]
    public object CreateBoxedDouble()
    {
        return 3.14159;
    }

    [Benchmark]
    public MetadataValue CreateBooleanValue()
    {
        return MetadataValue.FromBoolean(true);
    }

    [Benchmark]
    public object CreateBoxedBoolean()
    {
        return true;
    }

    [Benchmark]
    public MetadataValue CreateStringValue()
    {
        return MetadataValue.FromString("test");
    }
}
