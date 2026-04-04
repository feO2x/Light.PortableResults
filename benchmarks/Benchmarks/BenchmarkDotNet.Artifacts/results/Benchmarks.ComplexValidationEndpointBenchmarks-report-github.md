```

BenchmarkDotNet v0.15.8, macOS Tahoe 26.4 (25E246) [Darwin 25.4.0]
Apple M3 Max, 1 CPU, 16 logical and 16 physical cores
.NET SDK 10.0.103
  [Host]     : .NET 10.0.5 (10.0.5, 10.0.526.15411), Arm64 RyuJIT armv8.0-a
  DefaultJob : .NET 10.0.5 (10.0.5, 10.0.526.15411), Arm64 RyuJIT armv8.0-a


```

| Method                              |     Mean |   Error |  StdDev | Ratio | RatioSD |   Gen0 |   Gen1 | Allocated | Alloc Ratio |
|-------------------------------------|---------:|--------:|--------:|------:|--------:|-------:|-------:|----------:|------------:|
| FluentValidationEndpoint            | 397.6 ns | 2.80 ns | 2.34 ns |  1.00 |    0.01 | 0.2789 | 0.0010 |   2.28 KB |        1.00 |
| PortableResultsValidationEndpoint   |       NA |      NA |      NA |     ? |       ? |     NA |     NA |        NA |           ? |
| PortableResultsNestedValidationOnly |       NA |      NA |      NA |     ? |       ? |     NA |     NA |        NA |           ? |

Benchmarks with issues:
ComplexValidationEndpointBenchmarks.PortableResultsValidationEndpoint: DefaultJob
ComplexValidationEndpointBenchmarks.PortableResultsNestedValidationOnly: DefaultJob
