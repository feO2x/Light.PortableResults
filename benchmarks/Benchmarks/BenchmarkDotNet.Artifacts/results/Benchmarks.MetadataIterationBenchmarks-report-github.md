```

BenchmarkDotNet v0.15.8, macOS Tahoe 26.2 (25C56) [Darwin 25.2.0]
Apple M3 Max, 1 CPU, 16 logical and 16 physical cores
.NET SDK 10.0.101
  [Host]     : .NET 10.0.1 (10.0.1, 10.0.125.57005), Arm64 RyuJIT armv8.0-a
  DefaultJob : .NET 10.0.1 (10.0.1, 10.0.125.57005), Arm64 RyuJIT armv8.0-a


```

| Method                      |     Mean |     Error |    StdDev | Ratio | RatioSD | Allocated | Alloc Ratio |
|-----------------------------|---------:|----------:|----------:|------:|--------:|----------:|------------:|
| DictionaryIterate_Small     | 1.394 ns | 0.0020 ns | 0.0018 ns |  1.00 |    0.00 |         - |          NA |
| MetadataObjectIterate_Small | 2.846 ns | 0.0153 ns | 0.0143 ns |  2.04 |    0.01 |         - |          NA |
| DictionaryIterate_Large     | 5.136 ns | 0.0317 ns | 0.0281 ns |  3.68 |    0.02 |         - |          NA |
| MetadataObjectIterate_Large | 7.800 ns | 0.0230 ns | 0.0215 ns |  5.60 |    0.02 |         - |          NA |
