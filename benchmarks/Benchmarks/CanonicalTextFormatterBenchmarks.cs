using System;
using System.Globalization;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Engines;
using Light.PortableResults.Text;

namespace Benchmarks;

[MemoryDiagnoser]
[SimpleJob]
[GroupBenchmarksBy(BenchmarkDotNet.Configs.BenchmarkLogicalGroupRule.ByCategory)]
[CategoriesColumn]
public class CanonicalTextFormatterBenchmarks
{
    private readonly Consumer _consumer = new();
    private readonly DateTime[] _dateTimes =
    {
        DateTime.SpecifyKind(DateTime.MinValue, DateTimeKind.Utc),
        new DateTime(2026, 7, 26, 13, 45, 30, DateTimeKind.Utc).AddTicks(1_234_567),
        DateTime.SpecifyKind(DateTime.MaxValue, DateTimeKind.Utc)
    };
    private readonly Guid[] _guids =
    {
        Guid.Empty,
        new ("a1b2c3d4-e5f6-7890-abcd-ef1234567890"),
        new ("ffffffff-ffff-ffff-ffff-ffffffffffff")
    };

    [Benchmark(Baseline = true)]
    [BenchmarkCategory("Guid")]
    public void FrameworkGuidTryFormat()
    {
        Span<char> destination = stackalloc char[CanonicalTextFormatter.MaximumGuidLength];
        foreach (var value in _guids)
        {
            value.TryFormat(destination, out var charsWritten, "D");
            _consumer.Consume(charsWritten);
            _consumer.Consume(destination[0]);
        }
    }

    [Benchmark]
    [BenchmarkCategory("Guid")]
    public void CanonicalGuidTryFormat()
    {
        Span<char> destination = stackalloc char[CanonicalTextFormatter.MaximumGuidLength];
        foreach (var value in _guids)
        {
            CanonicalTextFormatter.TryFormat(value, destination, out var charsWritten);
            _consumer.Consume(charsWritten);
            _consumer.Consume(destination[0]);
        }
    }

    [Benchmark(Baseline = true)]
    [BenchmarkCategory("DateTime")]
    public void FrameworkDateTimeTryFormat()
    {
        Span<char> destination = stackalloc char[CanonicalTextFormatter.MaximumDateTimeLength];
        foreach (var value in _dateTimes)
        {
            value.TryFormat(
                destination,
                out var charsWritten,
                "yyyy-MM-dd'T'HH:mm:ss.FFFFFFFK",
                CultureInfo.InvariantCulture
            );
            _consumer.Consume(charsWritten);
            _consumer.Consume(destination[0]);
        }
    }

    [Benchmark]
    [BenchmarkCategory("DateTime")]
    public void CanonicalDateTimeTryFormat()
    {
        Span<char> destination = stackalloc char[CanonicalTextFormatter.MaximumDateTimeLength];
        foreach (var value in _dateTimes)
        {
            CanonicalTextFormatter.TryFormat(value, destination, out var charsWritten);
            _consumer.Consume(charsWritten);
            _consumer.Consume(destination[0]);
        }
    }
}
