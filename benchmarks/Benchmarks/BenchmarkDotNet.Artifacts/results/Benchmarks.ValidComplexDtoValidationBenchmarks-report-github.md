```

BenchmarkDotNet v0.15.8, macOS Tahoe 26.4 (25E246) [Darwin 25.4.0]
Apple M3 Max, 1 CPU, 16 logical and 16 physical cores
.NET SDK 10.0.103
  [Host]     : .NET 10.0.5 (10.0.5, 10.0.526.15411), Arm64 RyuJIT armv8.0-a
  DefaultJob : .NET 10.0.5 (10.0.5, 10.0.526.15411), Arm64 RyuJIT armv8.0-a


```
| Method                            | Mean       | Error    | StdDev   | Ratio | Gen0   | Gen1   | Allocated | Alloc Ratio |
|---------------------------------- |-----------:|---------:|---------:|------:|-------:|-------:|----------:|------------:|
| FluentValidationScopedOrTransient | 8,318.7 ns | 78.34 ns | 69.45 ns |  1.00 | 4.1504 | 0.1221 |  33.94 KB |        1.00 |
| FluentValidationSingleton         | 1,685.9 ns |  5.01 ns |  4.69 ns |  0.20 | 0.7057 | 0.0019 |   5.77 KB |        0.17 |
| LightPortableResults              |   742.2 ns |  7.40 ns |  6.93 ns |  0.09 | 0.1554 |      - |   1.27 KB |        0.04 |
