```

BenchmarkDotNet v0.15.8, macOS Tahoe 26.2 (25C56) [Darwin 25.2.0]
Apple M3 Max, 1 CPU, 16 logical and 16 physical cores
.NET SDK 10.0.102
  [Host]     : .NET 10.0.2 (10.0.2, 10.0.225.61305), Arm64 RyuJIT armv8.0-a
  Job-MEHJPP : .NET 10.0.2 (10.0.2, 10.0.225.61305), Arm64 RyuJIT armv8.0-a

IterationCount=5  WarmupCount=1

```

| Method                 |       Mean |    Error |   StdDev | Ratio | RatioSD |   Gen0 | Allocated | Alloc Ratio |
|------------------------|-----------:|---------:|---------:|------:|--------:|-------:|----------:|------------:|
| Errors1                |   153.1 ns | 10.13 ns |  2.63 ns |  1.00 |    0.02 | 0.0162 |     136 B |        1.00 |
| Errors3_UniqueTargets  |   343.8 ns |  9.14 ns |  2.37 ns |  2.25 |    0.04 | 0.0162 |     136 B |        1.00 |
| Errors3_SharedTarget   |   301.0 ns |  7.81 ns |  2.03 ns |  1.97 |    0.03 | 0.0162 |     136 B |        1.00 |
| Errors5_UniqueTargets  |   536.2 ns | 12.60 ns |  3.27 ns |  3.50 |    0.06 | 0.0162 |     136 B |        1.00 |
| Errors5_SharedTargets  |   491.2 ns | 14.16 ns |  3.68 ns |  3.21 |    0.05 | 0.0162 |     136 B |        1.00 |
| Errors10_UniqueTargets | 1,107.6 ns | 55.34 ns | 14.37 ns |  7.24 |    0.14 | 0.0153 |     136 B |        1.00 |
| Errors10_SharedTargets |   900.7 ns | 47.45 ns | 12.32 ns |  5.89 |    0.12 | 0.0162 |     136 B |        1.00 |
