using BenchmarkDotNet.Attributes;
using Light.PortableResults;
using Light.PortableResults.Validation;
using Light.PortableResults.Validation.Targeting;

namespace Benchmarks;

[MemoryDiagnoser]
public class ValidationContextScopeBenchmarks
{
    [Benchmark]
    public string CreateNestedScopes()
    {
        var context = ValidationBenchmarkHelpers.ValidationContextFactory.CreateValidationContext();
        var itemsContext = context.ForMember("items", isNormalized: true);

        return itemsContext.ForIndex(0).ForMember("sku", isNormalized: true).TargetPrefix;
    }
}

[MemoryDiagnoser]
public class ValidationErrorAccumulationBenchmarks
{
    [Benchmark]
    public Errors Accumulate1Error() => AccumulateErrors(1);

    [Benchmark]
    public Errors Accumulate2Errors() => AccumulateErrors(2);

    [Benchmark]
    public Errors Accumulate10Errors() => AccumulateErrors(10);

    [Benchmark]
    public Errors Accumulate11Errors() => AccumulateErrors(11);

    private static Errors AccumulateErrors(int errorCount)
    {
        var context = ValidationBenchmarkHelpers.ValidationContextFactory.CreateValidationContext();
        for (var i = 0; i < errorCount; i++)
        {
            context.AddError(
                $"message {i}",
                $"Code{i}",
                ValidationTarget.Relative($"items[{i}]", isNormalized: true)
            );
        }

        return context.Errors;
    }
}

[MemoryDiagnoser]
public class ValidationErrorMaterializationBenchmarks
{
    private ValidationContext _elevenErrorContext;
    private ValidationContext _oneErrorContext;
    private ValidationContext _tenErrorContext;
    private ValidationContext _twoErrorContext;

    [GlobalSetup]
    public void Setup()
    {
        _oneErrorContext = CreateContextWithErrors(1);
        _twoErrorContext = CreateContextWithErrors(2);
        _tenErrorContext = CreateContextWithErrors(10);
        _elevenErrorContext = CreateContextWithErrors(11);
    }

    [Benchmark]
    public Errors Materialize1Error() => _oneErrorContext.Errors;

    [Benchmark]
    public Errors Materialize2Errors() => _twoErrorContext.Errors;

    [Benchmark]
    public Errors Materialize10Errors() => _tenErrorContext.Errors;

    [Benchmark]
    public Errors Materialize11Errors() => _elevenErrorContext.Errors;

    private static ValidationContext CreateContextWithErrors(int errorCount)
    {
        var context = ValidationBenchmarkHelpers.ValidationContextFactory.CreateValidationContext();
        for (var i = 0; i < errorCount; i++)
        {
            context.AddError(
                $"message {i}",
                $"Code{i}",
                ValidationTarget.Relative($"items[{i}]", isNormalized: true)
            );
        }

        return context;
    }
}

internal static class ValidationBenchmarkHelpers
{
    public static readonly DefaultValidationContextFactory ValidationContextFactory = new ();
}
