```

BenchmarkDotNet v0.15.8, macOS Tahoe 26.4 (25E246) [Darwin 25.4.0]
Apple M3 Max, 1 CPU, 16 logical and 16 physical cores
.NET SDK 10.0.103
  [Host]     : .NET 10.0.5 (10.0.5, 10.0.526.15411), Arm64 RyuJIT armv8.0-a
  DefaultJob : .NET 10.0.5 (10.0.5, 10.0.526.15411), Arm64 RyuJIT armv8.0-a


```
| Method                            | Mean      | Error     | StdDev    | Ratio | Gen0   | Gen1   | Allocated | Alloc Ratio |
|---------------------------------- |----------:|----------:|----------:|------:|-------:|-------:|----------:|------------:|
| FluentValidationScopedOrTransient | 13.051 μs | 0.1363 μs | 0.1138 μs |  1.00 | 6.1035 | 0.3052 |  50.19 KB |        1.00 |
| FluentValidationSingleton         |  5.840 μs | 0.0181 μs | 0.0160 μs |  0.45 | 2.7008 | 0.0687 |   22.1 KB |        0.44 |
| LightPortableResults              |  1.450 μs | 0.0039 μs | 0.0034 μs |  0.11 | 0.2251 |      - |   1.84 KB |        0.04 |
