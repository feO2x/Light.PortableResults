using System.Collections.Generic;
using BenchmarkDotNet.Attributes;
using Light.PortableResults.Metadata;

namespace Benchmarks;

/// <summary>
/// Benchmarks for merge operations.
/// </summary>
[MemoryDiagnoser]
[SimpleJob]
public class MetadataMergeBenchmarks
{
    private MetadataObject _base;
    private Dictionary<string, object?> _baseDict = null!;
    private MetadataObject _overlay;
    private Dictionary<string, object?> _overlayDict = null!;

    [GlobalSetup]
    public void Setup()
    {
        _base = MetadataObject.Create(
            ("a", 1),
            ("b", 2),
            ("c", 3)
        );

        _overlay = MetadataObject.Create(
            ("c", 30),
            ("d", 4),
            ("e", 5)
        );

        _baseDict = new Dictionary<string, object?>
        {
            ["a"] = 1,
            ["b"] = 2,
            ["c"] = 3
        };

        _overlayDict = new Dictionary<string, object?>
        {
            ["c"] = 30,
            ["d"] = 4,
            ["e"] = 5
        };
    }

    [Benchmark(Baseline = true)]
    public Dictionary<string, object?> MergeDictionaries()
    {
        var result = new Dictionary<string, object?>(_baseDict);
        foreach (var kvp in _overlayDict)
        {
            result[kvp.Key] = kvp.Value;
        }

        return result;
    }

    [Benchmark]
    public MetadataObject MergeMetadataObjects()
    {
        return _base.Merge(_overlay);
    }
}
