```

BenchmarkDotNet v0.15.8, macOS Tahoe 26.2 (25C56) [Darwin 25.2.0]
Apple M3 Max, 1 CPU, 16 logical and 16 physical cores
.NET SDK 10.0.101
  [Host]     : .NET 10.0.1 (10.0.1, 10.0.125.57005), Arm64 RyuJIT armv8.0-a
  DefaultJob : .NET 10.0.1 (10.0.1, 10.0.125.57005), Arm64 RyuJIT armv8.0-a


```

| Method                    |      Mean |    Error |   StdDev | Ratio | RatioSD |   Gen0 |   Gen1 | Allocated | Alloc Ratio |
|---------------------------|----------:|---------:|---------:|------:|--------:|-------:|-------:|----------:|------------:|
| BuildDictionary           |  59.63 ns | 0.241 ns | 0.225 ns |  1.00 |    0.01 | 0.0640 |      - |     536 B |        1.00 |
| BuildMetadataObject       |  62.49 ns | 0.365 ns | 0.324 ns |  1.05 |    0.01 | 0.0257 |      - |     216 B |        0.40 |
| BuildDictionary_Large     | 462.14 ns | 2.050 ns | 1.917 ns |  7.75 |    0.04 | 0.3824 | 0.0033 |    3200 B |        5.97 |
| BuildMetadataObject_Large | 963.76 ns | 4.321 ns | 4.042 ns | 16.16 |    0.09 | 0.1621 |      - |    1368 B |        2.55 |
