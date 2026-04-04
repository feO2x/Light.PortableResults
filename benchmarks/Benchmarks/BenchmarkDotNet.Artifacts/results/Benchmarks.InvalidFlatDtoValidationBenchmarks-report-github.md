```

BenchmarkDotNet v0.15.8, macOS Tahoe 26.4 (25E246) [Darwin 25.4.0]
Apple M3 Max, 1 CPU, 16 logical and 16 physical cores
.NET SDK 10.0.103
  [Host]     : .NET 10.0.5 (10.0.5, 10.0.526.15411), Arm64 RyuJIT armv8.0-a
  DefaultJob : .NET 10.0.5 (10.0.5, 10.0.526.15411), Arm64 RyuJIT armv8.0-a


```

| Method                            |       Mean |    Error |   StdDev | Ratio | RatioSD |   Gen0 |   Gen1 | Allocated | Alloc Ratio |
|-----------------------------------|-----------:|---------:|---------:|------:|--------:|-------:|-------:|----------:|------------:|
| FluentValidationScopedOrTransient | 3,530.0 ns | 50.92 ns | 66.22 ns |  1.00 |    0.03 | 1.9226 | 0.0305 |   16192 B |        1.00 |
| FluentValidationSingleton         | 2,082.4 ns | 26.04 ns | 32.94 ns |  0.59 |    0.01 | 1.1559 | 0.0114 |    9696 B |        0.60 |
| LightPortableResults              |   289.3 ns |  0.69 ns |  0.64 ns |  0.08 |    0.00 | 0.1001 |      - |     840 B |        0.05 |
