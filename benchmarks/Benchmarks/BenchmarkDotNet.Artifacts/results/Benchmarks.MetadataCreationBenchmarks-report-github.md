```

BenchmarkDotNet v0.15.8, macOS Tahoe 26.2 (25C56) [Darwin 25.2.0]
Apple M3 Max, 1 CPU, 16 logical and 16 physical cores
.NET SDK 10.0.101
  [Host]     : .NET 10.0.1 (10.0.1, 10.0.125.57005), Arm64 RyuJIT armv8.0-a
  DefaultJob : .NET 10.0.1 (10.0.1, 10.0.125.57005), Arm64 RyuJIT armv8.0-a


```

| Method                     |      Mean |    Error |   StdDev | Ratio | RatioSD |   Gen0 |   Gen1 | Allocated | Alloc Ratio |
|----------------------------|----------:|---------:|---------:|------:|--------:|-------:|-------:|----------:|------------:|
| CreateDictionary_Small     |  34.50 ns | 0.111 ns | 0.104 ns |  1.00 |    0.00 | 0.0315 |      - |     264 B |        1.00 |
| CreateMetadataObject_Small |  38.55 ns | 0.088 ns | 0.082 ns |  1.12 |    0.00 | 0.0363 |      - |     304 B |        1.15 |
| CreateDictionary_Large     | 149.86 ns | 0.478 ns | 0.447 ns |  4.34 |    0.02 | 0.1357 | 0.0002 |    1136 B |        4.30 |
| CreateMetadataObject_Large | 177.22 ns | 0.652 ns | 0.610 ns |  5.14 |    0.02 | 0.0899 |      - |     752 B |        2.85 |
