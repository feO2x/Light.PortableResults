```

BenchmarkDotNet v0.15.8, macOS Tahoe 26.2 (25C56) [Darwin 25.2.0]
Apple M3 Max, 1 CPU, 16 logical and 16 physical cores
.NET SDK 10.0.101
  [Host]     : .NET 10.0.1 (10.0.1, 10.0.125.57005), Arm64 RyuJIT armv8.0-a
  DefaultJob : .NET 10.0.1 (10.0.1, 10.0.125.57005), Arm64 RyuJIT armv8.0-a


```

| Method               |       Mean |     Error |    StdDev |     Median | Ratio |   Gen0 | Allocated | Alloc Ratio |
|----------------------|-----------:|----------:|----------:|-----------:|------:|-------:|----------:|------------:|
| CreateObjectArray    | 18.5155 ns | 0.0190 ns | 0.0159 ns | 18.5196 ns | 1.000 | 0.0220 |     184 B |        1.00 |
| CreateMetadataArray  | 19.4645 ns | 0.0285 ns | 0.0267 ns | 19.4568 ns | 1.051 | 0.0373 |     312 B |        1.70 |
| ObjectArrayAccess    |  0.0000 ns | 0.0000 ns | 0.0000 ns |  0.0000 ns | 0.000 |      - |         - |        0.00 |
| MetadataArrayAccess  |  0.0000 ns | 0.0000 ns | 0.0000 ns |  0.0000 ns | 0.000 |      - |         - |        0.00 |
| ObjectArrayIterate   |  1.1528 ns | 0.0028 ns | 0.0023 ns |  1.1520 ns | 0.062 |      - |         - |        0.00 |
| MetadataArrayIterate |  3.7489 ns | 0.1020 ns | 0.2083 ns |  3.8496 ns | 0.202 |      - |         - |        0.00 |
