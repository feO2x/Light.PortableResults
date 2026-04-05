using BenchmarkDotNet.Attributes;
using Light.PortableResults;
using Light.PortableResults.Validation;

namespace Benchmarks;

[MemoryDiagnoser]
public class ValueNormalizerBenchmarks
{
    private readonly IValueNormalizer _normalizer = TrimStringNormalizer.Instance;
    private readonly string? _value = "  Alice  ";

    [Benchmark]
    public string? NormalizeTrimmedString() => _normalizer.Normalize(_value);
}

[MemoryDiagnoser]
public class AutomaticNullErrorProviderBenchmarks
{
    private ValidationErrorMessageContext<string?> _context;

    [GlobalSetup]
    public void Setup()
    {
        var validationContext = ValidationBenchmarkHelpers.ValidationContextFactory.CreateValidationContext();
        _context = validationContext
           .Check<string?>(null, NoOpValueNormalizer.Instance, target: "request", displayName: "request")
           .CreateMessageContext();
    }

    [Benchmark]
    public Error CreateDefaultAutomaticNullError()
    {
        DefaultAutomaticNullErrorProvider.Instance.TryCreateError(in _context, out var error);
        return error;
    }
}

[MemoryDiagnoser]
public class ValidationErrorMessageTemplateBenchmarks
{
    private readonly IValidationErrorMessageTemplate _constantTemplate =
        new ConstantValidationErrorMessageTemplate("The value must not be null");

    private readonly IValidationErrorMessageTemplate<int> _formattedTemplate =
        ValidationErrorTemplates.Default.MinLength;

    private readonly IValidationErrorMessageTemplate _valueTypeTemplate = new ValueTypeMessageTemplate();

    private ValidationErrorMessageContext<string> _stringContext;
    private ValidationErrorMessageContext<int> _valueTypeContext;

    [GlobalSetup]
    public void Setup()
    {
        var validationContext = ValidationBenchmarkHelpers.ValidationContextFactory.CreateValidationContext();
        _stringContext = validationContext
           .Check("Alice", target: "firstName", displayName: "firstName")
           .CreateMessageContext();
        _valueTypeContext = validationContext
           .Check(17, target: "age", displayName: "age")
           .CreateMessageContext();
    }

    [Benchmark]
    public ValidationErrorMessage CreateConstantMessage() => _constantTemplate.ProvideMessage(in _stringContext);

    [Benchmark]
    public ValidationErrorMessage CreateFormattedMessage() => _formattedTemplate.ProvideMessage(in _stringContext, 3);

    [Benchmark]
    public ValidationErrorMessage CreateValueTypeMessage() => _valueTypeTemplate.ProvideMessage(in _valueTypeContext);

    private sealed class ValueTypeMessageTemplate : IValidationErrorMessageTemplate
    {
        public bool IsMessageStable => false;

        public ValidationErrorMessage ProvideMessage<T>(in ValidationErrorMessageContext<T> context) =>
            new ($"{context.DisplayName} must be at least {context.Value}");
    }
}
