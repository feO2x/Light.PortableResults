```

BenchmarkDotNet v0.15.8, macOS Tahoe 26.4 (25E246) [Darwin 25.4.0]
Apple M3 Max, 1 CPU, 16 logical and 16 physical cores
.NET SDK 10.0.103
  [Host]     : .NET 10.0.5 (10.0.5, 10.0.526.15411), Arm64 RyuJIT armv8.0-a
  DefaultJob : .NET 10.0.5 (10.0.5, 10.0.526.15411), Arm64 RyuJIT armv8.0-a


```

| Method                            |     Mean |   Error |  StdDev | Ratio |   Gen0 |   Gen1 | Allocated | Alloc Ratio |
|-----------------------------------|---------:|--------:|--------:|------:|-------:|-------:|----------:|------------:|
| FluentValidationEndpoint          | 142.3 ns | 0.48 ns | 0.40 ns |  1.00 | 0.1338 | 0.0002 |    1120 B |        1.00 |
| PortableResultsValidationEndpoint | 163.0 ns | 0.56 ns | 0.47 ns |  1.15 | 0.1042 |      - |     872 B |        0.78 |
