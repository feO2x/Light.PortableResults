```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.4 LTS (Noble Numbat)
AMD Ryzen 9 5950X 3.40GHz, 1 CPU, 32 logical and 16 physical cores
.NET SDK 10.0.103
  [Host]     : .NET 10.0.3 (10.0.3, 10.0.326.7603), X64 RyuJIT x86-64-v3
  DefaultJob : .NET 10.0.3 (10.0.3, 10.0.326.7603), X64 RyuJIT x86-64-v3


```
| Method                                  | Mean     | Error    | StdDev   | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|---------------------------------------- |---------:|---------:|---------:|------:|--------:|-------:|----------:|------------:|
| ToCloudEvent_NonGenericSuccess          | 322.1 ns |  1.73 ns |  1.35 ns |  1.00 |    0.01 | 0.0243 |     407 B |        1.00 |
| ToCloudEvent_NonGenericFailure          | 572.9 ns |  6.33 ns |  5.92 ns |  1.78 |    0.02 | 0.0334 |     568 B |        1.40 |
| ToCloudEvent_GenericSuccess             | 561.6 ns |  6.01 ns |  5.62 ns |  1.74 |    0.02 | 0.0324 |     544 B |        1.34 |
| ToCloudEvent_GenericFailure             | 582.2 ns |  6.65 ns |  5.55 ns |  1.81 |    0.02 | 0.0334 |     567 B |        1.39 |
| ToCloudEvent_GenericSuccessWithMetadata | 943.4 ns | 12.35 ns | 11.55 ns |  2.93 |    0.04 | 0.0496 |     840 B |        2.06 |
