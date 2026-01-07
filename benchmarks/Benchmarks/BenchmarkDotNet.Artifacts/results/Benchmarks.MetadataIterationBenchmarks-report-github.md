```

BenchmarkDotNet v0.15.8, macOS Tahoe 26.2 (25C56) [Darwin 25.2.0]
Apple M3 Max, 1 CPU, 16 logical and 16 physical cores
.NET SDK 10.0.101
  [Host]     : .NET 10.0.1 (10.0.1, 10.0.125.57005), Arm64 RyuJIT armv8.0-a
  DefaultJob : .NET 10.0.1 (10.0.1, 10.0.125.57005), Arm64 RyuJIT armv8.0-a


```

| Method                      |      Mean |     Error |    StdDev |    Median | Ratio | RatioSD | Allocated | Alloc Ratio |
|-----------------------------|----------:|----------:|----------:|----------:|------:|--------:|----------:|------------:|
| DictionaryIterate_Small     | 1.3995 ns | 0.0145 ns | 0.0129 ns | 1.3971 ns |  1.00 |    0.01 |         - |          NA |
| MetadataObjectIterate_Small | 0.9278 ns | 0.0193 ns | 0.0181 ns | 0.9243 ns |  0.66 |    0.01 |         - |          NA |
| DictionaryIterate_Large     | 5.3035 ns | 0.0267 ns | 0.0249 ns | 5.3048 ns |  3.79 |    0.04 |         - |          NA |
| MetadataObjectIterate_Large | 5.8316 ns | 0.2393 ns | 0.7055 ns | 5.5857 ns |  4.17 |    0.50 |         - |          NA |
