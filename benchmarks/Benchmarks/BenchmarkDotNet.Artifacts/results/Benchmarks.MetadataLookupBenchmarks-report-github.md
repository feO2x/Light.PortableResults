```

BenchmarkDotNet v0.15.8, macOS Tahoe 26.2 (25C56) [Darwin 25.2.0]
Apple M3 Max, 1 CPU, 16 logical and 16 physical cores
.NET SDK 10.0.101
  [Host]     : .NET 10.0.1 (10.0.1, 10.0.125.57005), Arm64 RyuJIT armv8.0-a
  DefaultJob : .NET 10.0.1 (10.0.1, 10.0.125.57005), Arm64 RyuJIT armv8.0-a


```

| Method                     |     Mean |     Error |    StdDev | Ratio | RatioSD | Allocated | Alloc Ratio |
|----------------------------|---------:|----------:|----------:|------:|--------:|----------:|------------:|
| DictionaryLookup_Small     | 2.448 ns | 0.0154 ns | 0.0137 ns |  1.00 |    0.01 |         - |          NA |
| MetadataObjectLookup_Small | 2.427 ns | 0.0754 ns | 0.1540 ns |  0.99 |    0.06 |         - |          NA |
| DictionaryLookup_Large     | 2.418 ns | 0.0152 ns | 0.0142 ns |  0.99 |    0.01 |         - |          NA |
| MetadataObjectLookup_Large | 3.047 ns | 0.0279 ns | 0.0261 ns |  1.24 |    0.01 |         - |          NA |
