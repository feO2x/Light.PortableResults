```

BenchmarkDotNet v0.15.8, macOS Tahoe 26.4 (25E246) [Darwin 25.4.0]
Apple M3 Max, 1 CPU, 16 logical and 16 physical cores
.NET SDK 10.0.103
  [Host]     : .NET 10.0.5 (10.0.5, 10.0.526.15411), Arm64 RyuJIT armv8.0-a
  DefaultJob : .NET 10.0.5 (10.0.5, 10.0.526.15411), Arm64 RyuJIT armv8.0-a


```
| Method                            | Mean        | Error    | StdDev   | Ratio | Gen0   | Gen1   | Allocated | Alloc Ratio |
|---------------------------------- |------------:|---------:|---------:|------:|-------:|-------:|----------:|------------:|
| FluentValidationScopedOrTransient | 1,324.57 ns | 8.570 ns | 7.156 ns |  1.00 | 0.8316 | 0.0076 |    6984 B |        1.00 |
| FluentValidationSingleton         |   105.84 ns | 0.246 ns | 0.205 ns |  0.08 | 0.0755 | 0.0001 |     632 B |        0.09 |
| LightPortableResults              |    50.49 ns | 0.091 ns | 0.076 ns |  0.04 | 0.0124 |      - |     104 B |        0.01 |
