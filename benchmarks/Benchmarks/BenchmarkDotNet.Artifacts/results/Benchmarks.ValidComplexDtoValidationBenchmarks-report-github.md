```

BenchmarkDotNet v0.15.8, macOS Tahoe 26.4 (25E246) [Darwin 25.4.0]
Apple M3 Max, 1 CPU, 16 logical and 16 physical cores
.NET SDK 10.0.103
  [Host]     : .NET 10.0.5 (10.0.5, 10.0.526.15411), Arm64 RyuJIT armv8.0-a
  DefaultJob : .NET 10.0.5 (10.0.5, 10.0.526.15411), Arm64 RyuJIT armv8.0-a


```
| Method                            | Mean       | Error    | StdDev   | Ratio | Gen0   | Gen1   | Allocated | Alloc Ratio |
|---------------------------------- |-----------:|---------:|---------:|------:|-------:|-------:|----------:|------------:|
| FluentValidationScopedOrTransient | 8,401.1 ns | 54.96 ns | 45.89 ns |  1.00 | 4.2114 | 0.1526 |  34.51 KB |        1.00 |
| FluentValidationSingleton         | 1,674.5 ns |  6.65 ns |  6.22 ns |  0.20 | 0.7057 | 0.0019 |   5.77 KB |        0.17 |
| LightPortableResults              |   666.7 ns |  3.04 ns |  2.69 ns |  0.08 | 0.1326 |      - |   1.09 KB |        0.03 |
