using System.Text.RegularExpressions;
using FluentAssertions;
using Light.PortableResults.Metadata;
using Xunit;

namespace Light.PortableResults.Validation.Tests;

public sealed class ValidationErrorDefinitionTests
{
    [Fact]
    public void BuiltInDefinitions_ShouldExposeExpectedDefaults()
    {
        BuiltInValidationErrorDefinitions.NotNull.Code.Should().Be("NotNull");
        BuiltInValidationErrorDefinitions.Null.Code.Should().Be("Null");
        BuiltInValidationErrorDefinitions.Empty.Code.Should().Be("Empty");
        BuiltInValidationErrorDefinitions.NotEmpty.Code.Should().Be("NotEmpty");
        BuiltInValidationErrorDefinitions.NotNullOrWhiteSpace.Code.Should().Be("NotNullOrWhiteSpace");
        BuiltInValidationErrorDefinitions.Email.Code.Should().Be("Email");
        BuiltInValidationErrorDefinitions.Predicate.Code.Should().Be("Predicate");
        BuiltInValidationErrorDefinitions.NotNull.Metadata.Should().BeNull();
        BuiltInValidationErrorDefinitions.Email.Metadata.Should().BeNull();
    }

    [Fact]
    public void ParameterizedDefinitions_ShouldExposeExpectedMetadata()
    {
        var equalTo = BuiltInValidationErrorDefinitions.EqualTo("ABC");
        var lengthIn = BuiltInValidationErrorDefinitions.LengthIn(2, 5);
        var matches = BuiltInValidationErrorDefinitions.Matches("^[0-9]+$", RegexOptions.IgnoreCase);
        var precisionScale = BuiltInValidationErrorDefinitions.PrecisionScale(4, 2, ignoreTrailingZeros: true);

        equalTo.Metadata.Should().Be(
            MetadataObject.Create((ValidationErrorMetadataKeys.ComparativeValue, "ABC"))
        );
        lengthIn.Metadata.Should().Be(
            MetadataObject.Create(
                (ValidationErrorMetadataKeys.MinLength, 2),
                (ValidationErrorMetadataKeys.MaxLength, 5)
            )
        );
        matches.Metadata.Should().Be(
            MetadataObject.Create(
                (ValidationErrorMetadataKeys.Pattern, "^[0-9]+$"),
                (ValidationErrorMetadataKeys.RegexOptions, (int) RegexOptions.IgnoreCase)
            )
        );
        precisionScale.Metadata.Should().Be(
            MetadataObject.Create(
                (ValidationErrorMetadataKeys.ExpectedPrecision, 4),
                (ValidationErrorMetadataKeys.ExpectedScale, 2),
                (ValidationErrorMetadataKeys.IgnoreTrailingZeros, true)
            )
        );
    }

    [Fact]
    public void ValidationContexts_ShouldExposeSharedErrorDefinitionCache()
    {
        var sharedCache = new ValidationErrorDefinitionCache();
        var options = ValidationContextOptions.Default with { ErrorDefinitionCache = sharedCache };
        var context = new DefaultValidationContextFactory(options).CreateValidationContext();

        context.ErrorDefinitionCache.Should().BeSameAs(sharedCache);
        context.AsReadOnly().ErrorDefinitionCache.Should().BeSameAs(sharedCache);
    }

    [Fact]
    public void BuiltInDefinitionCache_ShouldReuseEquivalentDefinitionsAcrossFamilies()
    {
        var cache = new ValidationErrorDefinitionCache();

        BuiltInValidationErrorDefinitions.EqualTo(cache, "ABC").Should().BeSameAs(
            BuiltInValidationErrorDefinitions.EqualTo(cache, "ABC")
        );
        BuiltInValidationErrorDefinitions.IsIn(cache, 1, 10).Should().BeSameAs(
            BuiltInValidationErrorDefinitions.IsIn(cache, 1, 10)
        );
        BuiltInValidationErrorDefinitions.Matches(cache, "^[0-9]+$", RegexOptions.IgnoreCase).Should().BeSameAs(
            BuiltInValidationErrorDefinitions.Matches(cache, "^[0-9]+$", RegexOptions.IgnoreCase)
        );
        BuiltInValidationErrorDefinitions.Count(cache, 3).Should().BeSameAs(
            BuiltInValidationErrorDefinitions.Count(cache, 3)
        );
        BuiltInValidationErrorDefinitions.EnumName<TestStatus>(cache, ignoreCase: true).Should().BeSameAs(
            BuiltInValidationErrorDefinitions.EnumName<TestStatus>(cache, ignoreCase: true)
        );
        BuiltInValidationErrorDefinitions.PrecisionScale(cache, 4, 2, ignoreTrailingZeros: true).Should().BeSameAs(
            BuiltInValidationErrorDefinitions.PrecisionScale(cache, 4, 2, ignoreTrailingZeros: true)
        );
    }

    [Fact]
    public void CachedDefinitions_ShouldUseActiveTemplatesAcrossValidationRuns()
    {
        var sharedCache = new ValidationErrorDefinitionCache();
        var firstContext = CreateContext(
            sharedCache,
            ValidationErrorTemplates.Default with
            {
                EqualTo = new PrefixComparableTemplate("First run: ")
            }
        );
        var secondContext = CreateContext(
            sharedCache,
            ValidationErrorTemplates.Default with
            {
                EqualTo = new PrefixComparableTemplate("Second run: ")
            }
        );
        var definition = BuiltInValidationErrorDefinitions.EqualTo(firstContext.ErrorDefinitionCache, 18);

        firstContext.Check(10, target: "age", displayName: "Age").AddError(definition);
        secondContext.Check(10, target: "age", displayName: "Age").AddError(definition);

        firstContext.ToErrors()[0].Message.Should().Be("First run: Age = 18");
        secondContext.ToErrors()[0].Message.Should().Be("Second run: Age = 18");
    }

    [Fact]
    public void AddErrorDefinition_ShouldUseDefinitionDefaults_WhenNoOverridesAreProvided()
    {
        var context = new DefaultValidationContextFactory().CreateValidationContext();
        var definition = new TemplateValidationErrorDefinition(
            new ConstantValidationErrorMessageTemplate("Name must not be empty"),
            code: "NotEmpty",
            metadata: MetadataObject.Create(("minimumLength", 1)),
            category: ErrorCategory.Validation
        );
        var check = context.Check(string.Empty, target: "name", displayName: "Name").NormalizeTargetIfNecessary();

        check.AddError(definition);

        context.ToErrors().Should().Equal(
            new Errors(
                new Error
                {
                    Message = "Name must not be empty",
                    Code = "NotEmpty",
                    Target = "name",
                    Category = ErrorCategory.Validation,
                    Metadata = MetadataObject.Create(("minimumLength", 1))
                }
            )
        );
    }

    [Fact]
    public void AddErrorDefinition_ShouldTreatDefinitionTargetsAsAbsolute()
    {
        var context = new DefaultValidationContextFactory().CreateValidationContext();
        var childContext = context.ForMember("address", isNormalized: true);
        var definition = new TemplateValidationErrorDefinition(
            new ConstantValidationErrorMessageTemplate("Zip code is invalid"),
            target: ValidationTarget.Absolute("address.zipCode", isNormalized: true)
        );
        var check = childContext.Check("X", target: "zipCode", displayName: "Zip code").NormalizeTargetIfNecessary();

        check.AddError(definition);

        context.ToErrors().Should().Equal(
            new Errors(
                new Error
                {
                    Message = "Zip code is invalid",
                    Target = "address.zipCode",
                    Category = ErrorCategory.Validation
                }
            )
        );
    }

    [Fact]
    public void CustomDefinitions_ShouldBeImplementableWithoutInternalInfrastructure()
    {
        var context = new DefaultValidationContextFactory().CreateValidationContext();
        var definition = new ModuloValidationErrorDefinition(2);

        context.Check(3, target: "quantity", displayName: "Quantity").AddError(definition);

        context.ToErrors().Should().Equal(
            new Errors(
                new Error
                {
                    Message = "Quantity must be divisible by 2",
                    Code = "Modulo",
                    Target = "quantity",
                    Category = ErrorCategory.Validation,
                    Metadata = MetadataObject.Create(("divisor", 2))
                }
            )
        );
    }

    private static ValidationContext CreateContext(
        IValidationErrorDefinitionCache errorDefinitionCache,
        ValidationErrorTemplates errorTemplates
    )
    {
        var options = ValidationContextOptions.Default with
        {
            ErrorDefinitionCache = errorDefinitionCache,
            ErrorTemplates = errorTemplates
        };
        return new DefaultValidationContextFactory(options).CreateValidationContext();
    }

    private enum TestStatus
    {
        One,
        Two
    }

    private sealed class PrefixComparableTemplate : IComparableValidationErrorMessageTemplate
    {
        private readonly string _prefix;

        public PrefixComparableTemplate(string prefix) => _prefix = prefix;

        public ValidationErrorMessage ProvideMessage<T, TParameter>(
            in ValidationErrorMessageContext<T> context,
            TParameter parameter
        ) => new (_prefix + context.DisplayName + " = " + parameter);
    }

    private sealed class ModuloValidationErrorDefinition : ValidationErrorDefinition<int>
    {
        public ModuloValidationErrorDefinition(int divisor)
            : base(
                divisor,
                code: "Modulo",
                metadata: MetadataObject.Create(("divisor", divisor))
            ) { }

        public override ValidationErrorMessage ProvideMessage<T>(in ValidationErrorMessageContext<T> context) =>
            new ($"{context.DisplayName} must be divisible by {Parameter}");
    }
}
