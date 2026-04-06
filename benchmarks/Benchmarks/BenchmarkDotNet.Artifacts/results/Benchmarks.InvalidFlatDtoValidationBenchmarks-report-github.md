```

BenchmarkDotNet v0.15.8, macOS Tahoe 26.4 (25E246) [Darwin 25.4.0]
Apple M3 Max, 1 CPU, 16 logical and 16 physical cores
.NET SDK 10.0.103
  [Host]     : .NET 10.0.5 (10.0.5, 10.0.526.15411), Arm64 RyuJIT armv8.0-a
  DefaultJob : .NET 10.0.5 (10.0.5, 10.0.526.15411), Arm64 RyuJIT armv8.0-a


```
| Method                            | Mean       | Error    | StdDev  | Ratio | Gen0   | Gen1   | Allocated | Alloc Ratio |
|---------------------------------- |-----------:|---------:|--------:|------:|-------:|-------:|----------:|------------:|
| FluentValidationScopedOrTransient | 3,145.2 ns | 10.38 ns | 9.71 ns |  1.00 | 1.7509 | 0.0267 |   14672 B |        1.00 |
| FluentValidationSingleton         | 1,793.6 ns |  3.93 ns | 3.48 ns |  0.57 | 0.9937 | 0.0095 |    8320 B |        0.57 |
| LightPortableResults              |   289.6 ns |  0.57 ns | 0.51 ns |  0.09 | 0.0820 |      - |     688 B |        0.05 |
