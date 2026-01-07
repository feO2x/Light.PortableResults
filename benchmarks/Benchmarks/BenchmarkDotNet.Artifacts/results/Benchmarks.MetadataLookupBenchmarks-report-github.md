```

BenchmarkDotNet v0.15.8, macOS Tahoe 26.2 (25C56) [Darwin 25.2.0]
Apple M3 Max, 1 CPU, 16 logical and 16 physical cores
.NET SDK 10.0.101
  [Host]     : .NET 10.0.1 (10.0.1, 10.0.125.57005), Arm64 RyuJIT armv8.0-a
  DefaultJob : .NET 10.0.1 (10.0.1, 10.0.125.57005), Arm64 RyuJIT armv8.0-a


```

| Method                     |     Mean |     Error |    StdDev | Ratio | RatioSD | Allocated | Alloc Ratio |
|----------------------------|---------:|----------:|----------:|------:|--------:|----------:|------------:|
| DictionaryLookup_Small     | 2.674 ns | 0.0279 ns | 0.0261 ns |  1.00 |    0.01 |         - |          NA |
| MetadataObjectLookup_Small | 2.056 ns | 0.0512 ns | 0.0479 ns |  0.77 |    0.02 |         - |          NA |
| DictionaryLookup_Large     | 2.841 ns | 0.0139 ns | 0.0109 ns |  1.06 |    0.01 |         - |          NA |
| MetadataObjectLookup_Large | 2.872 ns | 0.0196 ns | 0.0183 ns |  1.07 |    0.01 |         - |          NA |
