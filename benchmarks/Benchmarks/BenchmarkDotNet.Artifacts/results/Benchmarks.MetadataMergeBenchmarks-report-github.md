```

BenchmarkDotNet v0.15.8, macOS Tahoe 26.2 (25C56) [Darwin 25.2.0]
Apple M3 Max, 1 CPU, 16 logical and 16 physical cores
.NET SDK 10.0.101
  [Host]     : .NET 10.0.1 (10.0.1, 10.0.125.57005), Arm64 RyuJIT armv8.0-a
  DefaultJob : .NET 10.0.1 (10.0.1, 10.0.125.57005), Arm64 RyuJIT armv8.0-a


```

| Method               |     Mean |    Error |   StdDev | Ratio |   Gen0 | Allocated | Alloc Ratio |
|----------------------|---------:|---------:|---------:|------:|-------:|----------:|------------:|
| MergeDictionaries    | 71.18 ns | 0.251 ns | 0.223 ns |  1.00 | 0.0554 |     464 B |        1.00 |
| MergeMetadataObjects | 85.61 ns | 0.431 ns | 0.403 ns |  1.20 | 0.0296 |     248 B |        0.53 |
