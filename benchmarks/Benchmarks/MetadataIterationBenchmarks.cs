using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using BenchmarkDotNet.Attributes;
using Light.Results.Metadata;

namespace Benchmarks;

[MemoryDiagnoser]
[SimpleJob]
[SuppressMessage("Performance", "CA1822:Mark members as static")]
public class MetadataIterationBenchmarks
{
    private Dictionary<string, object?> _dictionary = null!;
    private Dictionary<string, object?> _largeDictionary = null!;
    private MetadataObject _largeMetadataObject;
    private MetadataObject _metadataObject;

    [GlobalSetup]
    public void Setup()
    {
        _metadataObject = MetadataObject.Create(
            ("correlationId", "abc-123"),
            ("timestamp", 1704067200L),
            ("retryCount", 3)
        );

        _dictionary = new Dictionary<string, object?>
        {
            ["correlationId"] = "abc-123",
            ["timestamp"] = 1704067200L,
            ["retryCount"] = 3
        };

        _largeMetadataObject = MetadataObject.Create(
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

        _largeDictionary = new Dictionary<string, object?>
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

    [Benchmark(Baseline = true)]
    public int DictionaryIterate_Small()
    {
        var count = 0;
        foreach (var unused in _dictionary)
        {
            count++;
        }

        return count;
    }

    [Benchmark]
    public int MetadataObjectIterate_Small()
    {
        var count = 0;
        foreach (var unused in _metadataObject)
        {
            count++;
        }

        return count;
    }

    [Benchmark]
    public int DictionaryIterate_Large()
    {
        var count = 0;
        foreach (var unused in _largeDictionary)
        {
            count++;
        }

        return count;
    }

    [Benchmark]
    public int MetadataObjectIterate_Large()
    {
        var count = 0;
        foreach (var unused in _largeMetadataObject)
        {
            count++;
        }

        return count;
    }
}
