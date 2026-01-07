```

BenchmarkDotNet v0.15.8, macOS Tahoe 26.2 (25C56) [Darwin 25.2.0]
Apple M3 Max, 1 CPU, 16 logical and 16 physical cores
.NET SDK 10.0.101
  [Host]     : .NET 10.0.1 (10.0.1, 10.0.125.57005), Arm64 RyuJIT armv8.0-a
  DefaultJob : .NET 10.0.1 (10.0.1, 10.0.125.57005), Arm64 RyuJIT armv8.0-a


```

| Method                    |      Mean |    Error |   StdDev | Ratio | RatioSD |   Gen0 |   Gen1 | Allocated | Alloc Ratio |
|---------------------------|----------:|---------:|---------:|------:|--------:|-------:|-------:|----------:|------------:|
| BuildDictionary           |  61.61 ns | 0.342 ns | 0.304 ns |  1.00 |    0.01 | 0.0640 |      - |     536 B |        1.00 |
| BuildMetadataObject       |  39.42 ns | 0.238 ns | 0.223 ns |  0.64 |    0.00 | 0.0220 |      - |     184 B |        0.34 |
| BuildDictionary_Large     | 480.47 ns | 2.542 ns | 2.253 ns |  7.80 |    0.05 | 0.3824 | 0.0029 |    3200 B |        5.97 |
| BuildMetadataObject_Large | 552.73 ns | 1.572 ns | 1.313 ns |  8.97 |    0.05 | 0.1593 |      - |    1336 B |        2.49 |
