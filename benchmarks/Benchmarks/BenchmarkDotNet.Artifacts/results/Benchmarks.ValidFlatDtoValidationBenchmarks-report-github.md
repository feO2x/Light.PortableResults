```

BenchmarkDotNet v0.15.8, macOS Tahoe 26.4 (25E246) [Darwin 25.4.0]
Apple M3 Max, 1 CPU, 16 logical and 16 physical cores
.NET SDK 10.0.103
  [Host]     : .NET 10.0.5 (10.0.5, 10.0.526.15411), Arm64 RyuJIT armv8.0-a
  DefaultJob : .NET 10.0.5 (10.0.5, 10.0.526.15411), Arm64 RyuJIT armv8.0-a


```

| Method                            |        Mean |     Error |    StdDev | Ratio | RatioSD |   Gen0 |   Gen1 | Allocated | Alloc Ratio |
|-----------------------------------|------------:|----------:|----------:|------:|--------:|-------:|-------:|----------:|------------:|
| FluentValidationScopedOrTransient | 1,325.57 ns | 25.325 ns | 23.689 ns |  1.00 |    0.02 | 0.8316 | 0.0076 |    6984 B |        1.00 |
| FluentValidationSingleton         |   108.72 ns |  0.209 ns |  0.185 ns |  0.08 |    0.00 | 0.0755 | 0.0001 |     632 B |        0.09 |
| LightPortableResults              |    47.79 ns |  0.065 ns |  0.061 ns |  0.04 |    0.00 | 0.0124 |      - |     104 B |        0.01 |
