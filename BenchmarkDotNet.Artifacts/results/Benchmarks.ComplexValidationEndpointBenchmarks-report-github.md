```

BenchmarkDotNet v0.15.8, macOS Tahoe 26.3.1 (25D2128) [Darwin 25.3.0]
Apple M3 Max, 1 CPU, 16 logical and 16 physical cores
.NET SDK 10.0.103
  [Host]     : .NET 10.0.3 (10.0.3, 10.0.326.7603), Arm64 RyuJIT armv8.0-a
  DefaultJob : .NET 10.0.3 (10.0.3, 10.0.326.7603), Arm64 RyuJIT armv8.0-a


```
| Method                            | Mean     | Error   | StdDev  | Ratio | Gen0   | Gen1   | Allocated | Alloc Ratio |
|---------------------------------- |---------:|--------:|--------:|------:|-------:|-------:|----------:|------------:|
| FluentValidationEndpoint          | 422.5 ns | 3.29 ns | 2.91 ns |  1.00 | 0.2789 | 0.0010 |   2.28 KB |        1.00 |
| PortableResultsValidationEndpoint | 409.4 ns | 1.83 ns | 1.53 ns |  0.97 | 0.2913 | 0.0010 |   2.38 KB |        1.04 |
