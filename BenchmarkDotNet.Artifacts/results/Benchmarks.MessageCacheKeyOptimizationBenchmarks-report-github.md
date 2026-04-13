```

BenchmarkDotNet v0.15.8, macOS Tahoe 26.4 (25E246) [Darwin 25.4.0]
Apple M3 Max, 1 CPU, 16 logical and 16 physical cores
.NET SDK 10.0.103
  [Host]     : .NET 10.0.5 (10.0.5, 10.0.526.15411), Arm64 RyuJIT armv8.0-a
  Job-MEHJPP : .NET 10.0.5 (10.0.5, 10.0.526.15411), Arm64 RyuJIT armv8.0-a

IterationCount=5  WarmupCount=1  

```
| Method                       | Mean     | Error    | StdDev  | Ratio | RatioSD | Gen0   | Gen1   | Allocated | Alloc Ratio |
|----------------------------- |---------:|---------:|--------:|------:|--------:|-------:|-------:|----------:|------------:|
| NestedValidationWithoutCache | 347.5 ns | 17.07 ns | 4.43 ns |  1.00 |    0.02 | 0.2046 | 0.0010 |    1712 B |        1.00 |
| NestedValidationWithCache    | 340.9 ns |  4.09 ns | 1.06 ns |  0.98 |    0.01 | 0.0820 |      - |     688 B |        0.40 |
