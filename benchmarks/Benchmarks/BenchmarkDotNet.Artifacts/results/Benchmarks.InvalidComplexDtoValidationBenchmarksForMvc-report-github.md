```

BenchmarkDotNet v0.15.8, macOS Tahoe 26.4 (25E246) [Darwin 25.4.0]
Apple M3 Max, 1 CPU, 16 logical and 16 physical cores
.NET SDK 10.0.103
  [Host]     : .NET 10.0.5 (10.0.5, 10.0.526.15411), Arm64 RyuJIT armv8.0-a
  DefaultJob : .NET 10.0.5 (10.0.5, 10.0.526.15411), Arm64 RyuJIT armv8.0-a


```
| Method                            | Mean      | Error     | StdDev    | Ratio | Gen0   | Gen1   | Allocated | Alloc Ratio |
|---------------------------------- |----------:|----------:|----------:|------:|-------:|-------:|----------:|------------:|
| FluentValidationScopedOrTransient | 14.129 μs | 0.0498 μs | 0.0442 μs |  1.00 | 6.6528 | 0.3052 |  54.61 KB |        1.00 |
| FluentValidationSingleton         |  6.929 μs | 0.0272 μs | 0.0227 μs |  0.49 | 3.2578 | 0.0763 |  26.63 KB |        0.49 |
| LightPortableResults              |  1.515 μs | 0.0030 μs | 0.0025 μs |  0.11 | 0.2422 |      - |   1.99 KB |        0.04 |
