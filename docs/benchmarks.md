# Benchmarks

Light.PortableResults Validation is 2–6x faster than FluentValidation 12.1.1 with 8–22% of its allocations, compared with singleton-registered FluentValidation validators. This page shows the measurements behind that claim, the environment they ran in, and how to reproduce them.

**Package:** `Light.PortableResults.Validation`

## Baseline

The comparison uses `FluentValidationSingleton` as the baseline: validators of both libraries are reused as singletons. That is the fair comparison, because this documentation recommends singleton validators whenever they have no scoped dependencies, and FluentValidation validators can be registered the same way. The tables also contain `FluentValidationScopedOrTransient`, which creates a FluentValidation validator per validation, and against which the difference is much larger. The ranges in the summary are derived from the tables below:

| Scenario | Speed-up over `FluentValidationSingleton` | Allocations relative to `FluentValidationSingleton` |
| --- | ---: | ---: |
| Flat DTO, valid | 2.1x | 16% |
| Flat DTO, invalid | 6.2x | 8.3% |
| Complex DTO, valid | 2.3x | 22% |
| Complex DTO, invalid | 4.5x | 7.8% |

## Environment

All benchmarks ran on:

```
BenchmarkDotNet v0.15.8, macOS Tahoe 26.4 (25E246) [Darwin 25.4.0]
Apple M3 Max, 1 CPU, 16 logical and 16 physical cores
.NET SDK 10.0.103
  [Host]     : .NET 10.0.5 (10.0.5, 10.0.526.15411), Arm64 RyuJIT armv8.0-a
  DefaultJob : .NET 10.0.5 (10.0.5, 10.0.526.15411), Arm64 RyuJIT armv8.0-a
```

## Results

### Flat DTO — valid (no errors)

| Method                            | Mean        | Ratio | Allocated | Alloc Ratio |
|---------------------------------- |------------:|------:|----------:|------------:|
| FluentValidationScopedOrTransient | 1,324.57 ns |  1.00 |    6984 B |        1.00 |
| FluentValidationSingleton         |   105.84 ns |  0.08 |     632 B |        0.09 |
| LightPortableResults              |    50.49 ns |  0.04 |     104 B |        0.01 |

### Flat DTO — invalid (all three properties fail)

| Method                            | Mean       | Ratio | Allocated | Alloc Ratio |
|---------------------------------- |-----------:|------:|----------:|------------:|
| FluentValidationScopedOrTransient | 3,145.2 ns |  1.00 |   14672 B |        1.00 |
| FluentValidationSingleton         | 1,793.6 ns |  0.57 |    8320 B |        0.57 |
| LightPortableResults              |   289.6 ns |  0.09 |     688 B |        0.05 |

### Complex DTO — valid (one nested object, two nested collections, no errors)

| Method                            | Mean       | Ratio | Allocated | Alloc Ratio |
|---------------------------------- |-----------:|------:|----------:|------------:|
| FluentValidationScopedOrTransient | 8,318.7 ns |  1.00 |  33.94 KB |        1.00 |
| FluentValidationSingleton         | 1,685.9 ns |  0.20 |   5.77 KB |        0.17 |
| LightPortableResults              |   742.2 ns |  0.09 |   1.27 KB |        0.04 |

### Complex DTO — invalid (nine errors overall)

| Method                            |      Mean | Ratio | Allocated | Alloc Ratio |
|-----------------------------------|----------:|------:|----------:|------------:|
| FluentValidationScopedOrTransient | 13.985 μs |  1.00 |  53.45 KB |        1.00 |
| FluentValidationSingleton         |  6.755 μs |  0.48 |  25.47 KB |        0.48 |
| LightPortableResults              |  1.507 μs |  0.11 |   1.99 KB |        0.04 |

## Reproducing

See the [`benchmarks/Benchmarks`](../benchmarks/Benchmarks) project for the full benchmark source. Run selected benchmarks from the repository root with:

```bash
dotnet run -c Release --project benchmarks/Benchmarks -- --filter <glob>
```
