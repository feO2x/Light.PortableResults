```

BenchmarkDotNet v0.15.8, macOS Tahoe 26.2 (25C56) [Darwin 25.2.0]
Apple M3 Max, 1 CPU, 16 logical and 16 physical cores
.NET SDK 10.0.101
  [Host]     : .NET 10.0.1 (10.0.1, 10.0.125.57005), Arm64 RyuJIT armv8.0-a
  DefaultJob : .NET 10.0.1 (10.0.1, 10.0.125.57005), Arm64 RyuJIT armv8.0-a


```

| Method             |      Mean |     Error |    StdDev |   Gen0 | Allocated |
|--------------------|----------:|----------:|----------:|-------:|----------:|
| CreateInt64Value   | 0.0000 ns | 0.0000 ns | 0.0000 ns |      - |         - |
| CreateBoxedInt64   | 2.2388 ns | 0.0127 ns | 0.0113 ns | 0.0029 |      24 B |
| CreateDoubleValue  | 0.0000 ns | 0.0000 ns | 0.0000 ns |      - |         - |
| CreateBoxedDouble  | 2.2328 ns | 0.0108 ns | 0.0096 ns | 0.0029 |      24 B |
| CreateBooleanValue | 0.0000 ns | 0.0000 ns | 0.0000 ns |      - |         - |
| CreateBoxedBoolean | 2.2254 ns | 0.0133 ns | 0.0125 ns | 0.0029 |      24 B |
| CreateStringValue  | 0.0000 ns | 0.0000 ns | 0.0000 ns |      - |         - |
