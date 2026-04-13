```

BenchmarkDotNet v0.15.8, macOS Tahoe 26.4 (25E246) [Darwin 25.4.0]
Apple M3 Max, 1 CPU, 16 logical and 16 physical cores
.NET SDK 10.0.103
  [Host]     : .NET 10.0.5 (10.0.5, 10.0.526.15411), Arm64 RyuJIT armv8.0-a
  DefaultJob : .NET 10.0.5 (10.0.5, 10.0.526.15411), Arm64 RyuJIT armv8.0-a


```
| Method                            | Mean       | Error    | StdDev   | Ratio | Gen0   | Gen1   | Allocated | Alloc Ratio |
|---------------------------------- |-----------:|---------:|---------:|------:|-------:|-------:|----------:|------------:|
| FluentValidationScopedOrTransient | 3,965.8 ns | 23.03 ns | 21.54 ns |  1.00 | 2.0752 | 0.0305 |   17568 B |        1.00 |
| FluentValidationSingleton         | 2,385.5 ns |  6.70 ns |  6.27 ns |  0.60 | 1.3390 | 0.0114 |   11216 B |        0.64 |
| LightPortableResults              |   299.9 ns |  1.61 ns |  1.51 ns |  0.08 | 0.1001 |      - |     840 B |        0.05 |
