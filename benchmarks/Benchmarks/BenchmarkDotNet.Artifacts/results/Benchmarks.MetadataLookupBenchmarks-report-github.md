```

BenchmarkDotNet v0.15.8, macOS Tahoe 26.2 (25C56) [Darwin 25.2.0]
Apple M3 Max, 1 CPU, 16 logical and 16 physical cores
.NET SDK 10.0.101
  [Host]     : .NET 10.0.1 (10.0.1, 10.0.125.57005), Arm64 RyuJIT armv8.0-a
  DefaultJob : .NET 10.0.1 (10.0.1, 10.0.125.57005), Arm64 RyuJIT armv8.0-a


```

| Method                     |     Mean |     Error |    StdDev | Ratio | RatioSD | Allocated | Alloc Ratio |
|----------------------------|---------:|----------:|----------:|------:|--------:|----------:|------------:|
| DictionaryLookup_Small     | 2.506 ns | 0.0212 ns | 0.0198 ns |  1.00 |    0.01 |         - |          NA |
| MetadataObjectLookup_Small | 2.282 ns | 0.0748 ns | 0.0973 ns |  0.91 |    0.04 |         - |          NA |
| DictionaryLookup_Large     | 2.850 ns | 0.0449 ns | 0.0420 ns |  1.14 |    0.02 |         - |          NA |
| MetadataObjectLookup_Large | 3.043 ns | 0.0108 ns | 0.0096 ns |  1.21 |    0.01 |         - |          NA |
