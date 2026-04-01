using BenchmarkDotNet.Attributes;
using Light.PortableResults.Validation;

namespace Benchmarks;

[MemoryDiagnoser]
public class MessageCacheKeyOptimizationBenchmarks
{
    private DefaultValidationContextFactory _factoryWithCache = null!;
    private DefaultValidationContextFactory _factoryWithoutCache = null!;

    [GlobalSetup]
    public void Setup()
    {
        _factoryWithCache = new DefaultValidationContextFactory();
        var options = ValidationContextOptions.Default with
        {
            ErrorTemplates = new ValidationErrorTemplates { MessageCache = null }
        };
        _factoryWithoutCache = new DefaultValidationContextFactory(options);
    }

    [Benchmark(Baseline = true)]
    public int NestedValidationWithoutCache()
    {
        var context = _factoryWithoutCache.CreateValidationContext();
        var addressContext = context.ForMember("address", isNormalized: true);
        addressContext.Check<string?>(string.Empty, target: "zipCode").IsNotNullOrWhiteSpace();
        addressContext.Check<string?>(string.Empty, target: "city").IsNotNullOrWhiteSpace();
        addressContext.Check<string?>(string.Empty, target: "street").IsNotNullOrWhiteSpace();

        var billingContext = context.ForMember("billing", isNormalized: true);
        billingContext.Check<string?>(string.Empty, target: "zipCode").IsNotNullOrWhiteSpace();
        billingContext.Check<string?>(string.Empty, target: "city").IsNotNullOrWhiteSpace();
        billingContext.Check<string?>(string.Empty, target: "street").IsNotNullOrWhiteSpace();

        return context.Errors.Count;
    }

    [Benchmark]
    public int NestedValidationWithCache()
    {
        var context = _factoryWithCache.CreateValidationContext();
        var addressContext = context.ForMember("address", isNormalized: true);
        addressContext.Check<string?>(string.Empty, target: "zipCode").IsNotNullOrWhiteSpace();
        addressContext.Check<string?>(string.Empty, target: "city").IsNotNullOrWhiteSpace();
        addressContext.Check<string?>(string.Empty, target: "street").IsNotNullOrWhiteSpace();

        var billingContext = context.ForMember("billing", isNormalized: true);
        billingContext.Check<string?>(string.Empty, target: "zipCode").IsNotNullOrWhiteSpace();
        billingContext.Check<string?>(string.Empty, target: "city").IsNotNullOrWhiteSpace();
        billingContext.Check<string?>(string.Empty, target: "street").IsNotNullOrWhiteSpace();

        return context.Errors.Count;
    }
}
