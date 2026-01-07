```

BenchmarkDotNet v0.15.8, macOS Tahoe 26.2 (25C56) [Darwin 25.2.0]
Apple M3 Max, 1 CPU, 16 logical and 16 physical cores
.NET SDK 10.0.101
  [Host]     : .NET 10.0.1 (10.0.1, 10.0.125.57005), Arm64 RyuJIT armv8.0-a
  DefaultJob : .NET 10.0.1 (10.0.1, 10.0.125.57005), Arm64 RyuJIT armv8.0-a


```

| Method                    |      Mean |    Error |   StdDev | Ratio | RatioSD |   Gen0 |   Gen1 | Allocated | Alloc Ratio |
|---------------------------|----------:|---------:|---------:|------:|--------:|-------:|-------:|----------:|------------:|
| BuildDictionary           |  63.45 ns | 0.899 ns | 0.841 ns |  1.00 |    0.02 | 0.0640 |      - |     536 B |        1.00 |
| BuildMetadataObject       |  54.61 ns | 0.573 ns | 0.536 ns |  0.86 |    0.01 | 0.0258 |      - |     216 B |        0.40 |
| BuildDictionary_Large     | 523.05 ns | 1.787 ns | 1.672 ns |  8.24 |    0.11 | 0.3824 | 0.0029 |    3200 B |        5.97 |
| BuildMetadataObject_Large | 664.89 ns | 1.330 ns | 1.179 ns | 10.48 |    0.14 | 0.1631 |      - |    1368 B |        2.55 |
