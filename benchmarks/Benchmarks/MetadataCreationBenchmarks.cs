using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using BenchmarkDotNet.Attributes;
using Light.PortableResults.Metadata;

namespace Benchmarks;

[MemoryDiagnoser]
[SimpleJob]
[SuppressMessage("Performance", "CA1822:Mark members as static")]
public class MetadataCreationBenchmarks
{
    [Benchmark(Baseline = true)]
    public Dictionary<string, object?> CreateDictionary_Small()
    {
        return new Dictionary<string, object?>
        {
            ["correlationId"] = "abc-123",
            ["timestamp"] = 1704067200L,
            ["retryCount"] = 3
        };
    }

    [Benchmark]
    public MetadataObject CreateMetadataObject_Small()
    {
        return MetadataObject.Create(
            ("correlationId", "abc-123"),
            ("timestamp", 1704067200L),
            ("retryCount", 3)
        );
    }

    [Benchmark]
    public Dictionary<string, object?> CreateDictionary_Large()
    {
        return new Dictionary<string, object?>
        {
            ["prop1"] = "value1",
            ["prop2"] = "value2",
            ["prop3"] = "value3",
            ["prop4"] = 100L,
            ["prop5"] = 200L,
            ["prop6"] = 300L,
            ["prop7"] = true,
            ["prop8"] = false,
            ["prop9"] = 3.14,
            ["prop10"] = "final"
        };
    }

    [Benchmark]
    public MetadataObject CreateMetadataObject_Large()
    {
        return MetadataObject.Create(
            ("prop1", "value1"),
            ("prop2", "value2"),
            ("prop3", "value3"),
            ("prop4", 100L),
            ("prop5", 200L),
            ("prop6", 300L),
            ("prop7", true),
            ("prop8", false),
            ("prop9", 3.14),
            ("prop10", "final")
        );
    }
}
