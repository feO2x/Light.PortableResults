```

BenchmarkDotNet v0.15.8, macOS Tahoe 26.4 (25E246) [Darwin 25.4.0]
Apple M3 Max, 1 CPU, 16 logical and 16 physical cores
.NET SDK 10.0.103
  [Host]     : .NET 10.0.5 (10.0.5, 10.0.526.15411), Arm64 RyuJIT armv8.0-a
  DefaultJob : .NET 10.0.5 (10.0.5, 10.0.526.15411), Arm64 RyuJIT armv8.0-a


```
| Method                            | Mean      | Error     | StdDev    | Ratio | Gen0   | Gen1   | Allocated | Alloc Ratio |
|---------------------------------- |----------:|----------:|----------:|------:|-------:|-------:|----------:|------------:|
| FluentValidationScopedOrTransient | 14.363 μs | 0.1227 μs | 0.0958 μs |  1.00 | 6.6528 | 0.3052 |  54.75 KB |        1.00 |
| FluentValidationSingleton         |  6.948 μs | 0.0548 μs | 0.0458 μs |  0.48 | 3.2578 | 0.0763 |  26.63 KB |        0.49 |
| LightPortableResults              |  1.436 μs | 0.0014 μs | 0.0012 μs |  0.10 | 0.2422 |      - |   1.99 KB |        0.04 |
