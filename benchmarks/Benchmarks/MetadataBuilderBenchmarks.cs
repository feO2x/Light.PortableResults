using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using BenchmarkDotNet.Attributes;
using Light.PortableResults.Metadata;

namespace Benchmarks;

/// <summary>
/// Benchmarks for builder patterns comparing pooled vs non-pooled allocation.
/// </summary>
[MemoryDiagnoser]
[SimpleJob]
[SuppressMessage("Performance", "CA1822:Mark members as static")] // benchmark methods must not be static
public class MetadataBuilderBenchmarks
{
    [Benchmark(Baseline = true)]
    public Dictionary<string, object?> BuildDictionary()
    {
        var dict = new Dictionary<string, object?>
        {
            ["key1"] = "value1",
            ["key2"] = 42L,
            ["key3"] = true,
            ["key4"] = 3.14
        };
        return dict;
    }

    [Benchmark]
    public MetadataObject BuildMetadataObject()
    {
        using var builder = MetadataObjectBuilder.Create();
        builder.Add("key1", "value1");
        builder.Add("key2", 42L);
        builder.Add("key3", true);
        builder.Add("key4", 3.14);
        return builder.Build();
    }

    [Benchmark]
    public Dictionary<string, object?> BuildDictionary_Large()
    {
        var dict = new Dictionary<string, object?>();
        for (var i = 0; i < 20; i++)
        {
            dict[$"key{i}"] = i;
        }

        return dict;
    }

    [Benchmark]
    public MetadataObject BuildMetadataObject_Large()
    {
        using var builder = MetadataObjectBuilder.Create(20);
        for (var i = 0; i < 20; i++)
        {
            builder.Add($"key{i}", i);
        }

        return builder.Build();
    }
}
