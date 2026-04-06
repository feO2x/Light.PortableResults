```

BenchmarkDotNet v0.15.8, macOS Tahoe 26.4 (25E246) [Darwin 25.4.0]
Apple M3 Max, 1 CPU, 16 logical and 16 physical cores
.NET SDK 10.0.103
  [Host]     : .NET 10.0.5 (10.0.5, 10.0.526.15411), Arm64 RyuJIT armv8.0-a
  DefaultJob : .NET 10.0.5 (10.0.5, 10.0.526.15411), Arm64 RyuJIT armv8.0-a


```
| Method                            | Mean      | Error     | StdDev    | Ratio | Gen0   | Gen1   | Allocated | Alloc Ratio |
|---------------------------------- |----------:|----------:|----------:|------:|-------:|-------:|----------:|------------:|
| FluentValidationScopedOrTransient | 13.985 μs | 0.0705 μs | 0.0625 μs |  1.00 | 6.5308 | 0.3052 |  53.45 KB |        1.00 |
| FluentValidationSingleton         |  6.755 μs | 0.0410 μs | 0.0343 μs |  0.48 | 3.1128 | 0.0763 |  25.47 KB |        0.48 |
| LightPortableResults              |  1.507 μs | 0.0019 μs | 0.0018 μs |  0.11 | 0.2422 |      - |   1.99 KB |        0.04 |
