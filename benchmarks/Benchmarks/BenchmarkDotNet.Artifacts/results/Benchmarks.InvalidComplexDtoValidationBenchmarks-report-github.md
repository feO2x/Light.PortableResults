```

BenchmarkDotNet v0.15.8, macOS Tahoe 26.4 (25E246) [Darwin 25.4.0]
Apple M3 Max, 1 CPU, 16 logical and 16 physical cores
.NET SDK 10.0.103
  [Host]     : .NET 10.0.5 (10.0.5, 10.0.526.15411), Arm64 RyuJIT armv8.0-a
  DefaultJob : .NET 10.0.5 (10.0.5, 10.0.526.15411), Arm64 RyuJIT armv8.0-a


```
| Method                            | Mean      | Error     | StdDev    | Ratio | RatioSD | Gen0   | Gen1   | Allocated | Alloc Ratio |
|---------------------------------- |----------:|----------:|----------:|------:|--------:|-------:|-------:|----------:|------------:|
| FluentValidationScopedOrTransient | 13.270 μs | 0.2573 μs | 0.2963 μs |  1.00 |    0.03 | 6.1035 | 0.2441 |  50.22 KB |        1.00 |
| FluentValidationSingleton         |  5.901 μs | 0.0560 μs | 0.0467 μs |  0.44 |    0.01 | 2.7008 | 0.0687 |   22.1 KB |        0.44 |
| LightPortableResults              |  1.549 μs | 0.0036 μs | 0.0032 μs |  0.12 |    0.00 | 0.2251 |      - |   1.84 KB |        0.04 |
