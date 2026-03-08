```

BenchmarkDotNet v0.15.8, macOS Tahoe 26.3.1 (25D2128) [Darwin 25.3.0]
Apple M3 Max, 1 CPU, 16 logical and 16 physical cores
.NET SDK 10.0.103
  [Host]     : .NET 10.0.3 (10.0.3, 10.0.326.7603), Arm64 RyuJIT armv8.0-a
  DefaultJob : .NET 10.0.3 (10.0.3, 10.0.326.7603), Arm64 RyuJIT armv8.0-a


```
| Method                            | Mean     | Error   | StdDev  | Ratio | Gen0   | Gen1   | Allocated | Alloc Ratio |
|---------------------------------- |---------:|--------:|--------:|------:|-------:|-------:|----------:|------------:|
| FluentValidationEndpoint          | 147.7 ns | 0.85 ns | 0.80 ns |  1.00 | 0.1338 | 0.0002 |    1120 B |        1.00 |
| PortableResultsValidationEndpoint | 100.2 ns | 0.58 ns | 0.54 ns |  0.68 | 0.0889 |      - |     744 B |        0.66 |
