```

BenchmarkDotNet v0.15.8, macOS Tahoe 26.4 (25E246) [Darwin 25.4.0]
Apple M3 Max, 1 CPU, 16 logical and 16 physical cores
.NET SDK 10.0.103
  [Host]     : .NET 10.0.5 (10.0.5, 10.0.526.15411), Arm64 RyuJIT armv8.0-a
  DefaultJob : .NET 10.0.5 (10.0.5, 10.0.526.15411), Arm64 RyuJIT armv8.0-a


```

| Method                            |       Mean |    Error |   StdDev | Ratio |   Gen0 |   Gen1 | Allocated | Alloc Ratio |
|-----------------------------------|-----------:|---------:|---------:|------:|-------:|-------:|----------:|------------:|
| FluentValidationScopedOrTransient | 4,010.6 ns | 13.36 ns | 12.50 ns |  1.00 | 2.1057 | 0.0305 |   17680 B |        1.00 |
| FluentValidationSingleton         | 2,377.0 ns |  9.12 ns |  7.12 ns |  0.59 | 1.3390 | 0.0114 |   11216 B |        0.63 |
| LightPortableResults              |   288.1 ns |  0.32 ns |  0.27 ns |  0.07 | 0.1001 |      - |     840 B |        0.05 |
