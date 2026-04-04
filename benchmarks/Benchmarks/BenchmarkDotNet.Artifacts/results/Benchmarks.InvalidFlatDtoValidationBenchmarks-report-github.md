```

BenchmarkDotNet v0.15.8, macOS Tahoe 26.4 (25E246) [Darwin 25.4.0]
Apple M3 Max, 1 CPU, 16 logical and 16 physical cores
.NET SDK 10.0.103
  [Host]     : .NET 10.0.5 (10.0.5, 10.0.526.15411), Arm64 RyuJIT armv8.0-a
  DefaultJob : .NET 10.0.5 (10.0.5, 10.0.526.15411), Arm64 RyuJIT armv8.0-a


```

| Method                            |       Mean |    Error |   StdDev |     Median | Ratio |   Gen0 |   Gen1 | Allocated | Alloc Ratio |
|-----------------------------------|-----------:|---------:|---------:|-----------:|------:|-------:|-------:|----------:|------------:|
| FluentValidationScopedOrTransient | 3,162.1 ns | 24.82 ns | 22.01 ns | 3,153.7 ns |  1.00 | 1.7395 | 0.0153 |   14672 B |        1.00 |
| FluentValidationSingleton         | 1,786.9 ns |  6.96 ns |  6.17 ns | 1,786.4 ns |  0.57 | 0.9937 | 0.0095 |    8320 B |        0.57 |
| LightPortableResults              |   284.9 ns |  5.65 ns |  7.34 ns |   280.3 ns |  0.09 | 0.0820 |      - |     688 B |        0.05 |
