```

BenchmarkDotNet v0.15.8, macOS Tahoe 26.2 (25C56) [Darwin 25.2.0]
Apple M3 Max, 1 CPU, 16 logical and 16 physical cores
.NET SDK 10.0.102
  [Host]     : .NET 10.0.2 (10.0.2, 10.0.225.61305), Arm64 RyuJIT armv8.0-a
  Job-MEHJPP : .NET 10.0.2 (10.0.2, 10.0.225.61305), Arm64 RyuJIT armv8.0-a

IterationCount=5  WarmupCount=1

```

| Method                 |       Mean |    Error |  StdDev | Ratio | RatioSD |   Gen0 | Allocated | Alloc Ratio |
|------------------------|-----------:|---------:|--------:|------:|--------:|-------:|----------:|------------:|
| Errors1                |   154.9 ns |  1.88 ns | 0.49 ns |  1.00 |    0.00 | 0.0162 |     136 B |        1.00 |
| Errors3_UniqueTargets  |   366.1 ns | 10.58 ns | 2.75 ns |  2.36 |    0.02 | 0.0162 |     136 B |        1.00 |
| Errors3_SharedTarget   |   316.9 ns |  2.78 ns | 0.72 ns |  2.05 |    0.01 | 0.0162 |     136 B |        1.00 |
| Errors5_UniqueTargets  |   580.4 ns |  5.14 ns | 0.79 ns |  3.75 |    0.01 | 0.0162 |     136 B |        1.00 |
| Errors5_SharedTargets  |   522.6 ns | 13.76 ns | 3.57 ns |  3.37 |    0.02 | 0.0162 |     136 B |        1.00 |
| Errors10_UniqueTargets | 1,258.1 ns |  1.30 ns | 0.20 ns |  8.12 |    0.02 | 0.0153 |     136 B |        1.00 |
| Errors10_SharedTargets |   972.4 ns | 21.79 ns | 5.66 ns |  6.28 |    0.04 | 0.0153 |     136 B |        1.00 |
