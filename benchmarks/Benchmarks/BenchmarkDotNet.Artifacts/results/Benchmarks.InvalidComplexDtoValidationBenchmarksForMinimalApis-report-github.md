```

BenchmarkDotNet v0.15.8, macOS Tahoe 26.4 (25E246) [Darwin 25.4.0]
Apple M3 Max, 1 CPU, 16 logical and 16 physical cores
.NET SDK 10.0.103
  [Host]     : .NET 10.0.5 (10.0.5, 10.0.526.15411), Arm64 RyuJIT armv8.0-a
  DefaultJob : .NET 10.0.5 (10.0.5, 10.0.526.15411), Arm64 RyuJIT armv8.0-a


```
| Method                            | Mean      | Error     | StdDev    | Ratio | Gen0   | Gen1   | Allocated | Alloc Ratio |
|---------------------------------- |----------:|----------:|----------:|------:|-------:|-------:|----------:|------------:|
| FluentValidationScopedOrTransient | 14.145 μs | 0.0560 μs | 0.0524 μs |  1.00 | 6.5308 | 0.3052 |  53.77 KB |        1.00 |
| FluentValidationSingleton         |  6.778 μs | 0.0210 μs | 0.0187 μs |  0.48 | 3.1128 | 0.0763 |  25.47 KB |        0.47 |
| LightPortableResults              |  1.423 μs | 0.0041 μs | 0.0037 μs |  0.10 | 0.2422 |      - |   1.99 KB |        0.04 |
