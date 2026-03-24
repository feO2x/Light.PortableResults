using FluentAssertions;
using Light.PortableResults.Metadata;
using Xunit;

namespace Light.PortableResults.Validation.Tests;

public sealed class ValidationErrorDefinitionTests
{
    [Fact]
    public void BuiltInDefinitions_ShouldExposeExpectedDefaults()
    {
        var notNullDefinition = BuiltInValidationErrorDefinitions.NotNull;
        var greaterThanDefinition = BuiltInValidationErrorDefinitions.GreaterThan(18);
        var lessThanDefinition = BuiltInValidationErrorDefinitions.LessThan(65);
        var inDefinition = BuiltInValidationErrorDefinitions.IsIn(18, 65);

        notNullDefinition.Code.Should().Be("NotNull");
        notNullDefinition.Category.Should().Be(ErrorCategory.Validation);
        notNullDefinition.Metadata.Should().BeNull();

        greaterThanDefinition.Code.Should().Be("GreaterThan");
        greaterThanDefinition.Category.Should().Be(ErrorCategory.Validation);
        greaterThanDefinition.Metadata.Should().Be(
            MetadataObject.Create((ValidationErrorMetadataKeys.ComparativeValue, 18))
        );

        lessThanDefinition.Code.Should().Be("LessThan");
        lessThanDefinition.Category.Should().Be(ErrorCategory.Validation);
        lessThanDefinition.Metadata.Should().Be(
            MetadataObject.Create((ValidationErrorMetadataKeys.ComparativeValue, 65))
        );

        inDefinition.Code.Should().Be("IsIn");
        inDefinition.Category.Should().Be(ErrorCategory.Validation);
        inDefinition.Metadata.Should().Be(
            MetadataObject.Create(
                (ValidationErrorMetadataKeys.LowerBoundary, 18),
                (ValidationErrorMetadataKeys.UpperBoundary, 65)
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

        var expectedError = new Error
        {
            Message = "Name must not be empty",
            Code = "NotEmpty",
            Target = "name",
            Category = ErrorCategory.Validation,
            Metadata = MetadataObject.Create(("minimumLength", 1))
        };
        context.ToErrors().Should().Equal(new Errors(expectedError));
    }

    [Fact]
    public void AddErrorDefinition_ShouldAllowOverridingDefinitionDefaults()
    {
        var context = new DefaultValidationContextFactory().CreateValidationContext();
        var childContext = context.ForMember("address", isNormalized: true);
        var definition = new TemplateValidationErrorDefinition(
            new ConstantValidationErrorMessageTemplate("Zip code is invalid"),
            code: "InvalidZipCode",
            metadata: MetadataObject.Create(("hint", (MetadataValue) "definition")),
            target: ValidationTarget.Absolute("orders[2].postalCode", isNormalized: true),
            category: ErrorCategory.Validation
        );
        var overrideMetadata = MetadataObject.Create(("hint", (MetadataValue) "override"));
        var check = childContext.Check("X", target: "zipCode", displayName: "Zip code").NormalizeTargetIfNecessary();

        check.AddError(
            definition,
            code: "Overridden",
            metadata: overrideMetadata,
            target: ValidationTarget.Relative("postalCode", isNormalized: true),
            category: ErrorCategory.Conflict
        );

        var expectedError = new Error
        {
            Message = "Zip code is invalid",
            Code = "Overridden",
            Target = "address.postalCode",
            Category = ErrorCategory.Conflict,
            Metadata = overrideMetadata
        };
        context.ToErrors().Should().Equal(new Errors(expectedError));
    }

    [Fact]
    public void AddErrorDefinition_ShouldPreserveNormalizedMultiSegmentOverrideTargets()
    {
        var context = new DefaultValidationContextFactory().CreateValidationContext();
        var childContext = context.ForMember("customer", isNormalized: true);
        var definition = new TemplateValidationErrorDefinition(
            new ConstantValidationErrorMessageTemplate("Postal code is invalid")
        );
        var check = childContext
           .Check("X", target: "address.zipCode", displayName: "Postal code")
           .NormalizeTargetIfNecessary();

        check.AddError(definition, target: ValidationTarget.Relative("address.postalCode", isNormalized: true));

        context.ToErrors().Should().Equal(
            new Errors(
                new Error
                {
                    Message = "Postal code is invalid",
                    Target = "customer.address.postalCode",
                    Category = ErrorCategory.Validation
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
    public void BuiltInDefinitionCache_ShouldReuseEquivalentParameterizedDefinitions()
    {
        var cache = new ValidationErrorDefinitionCache();

        var firstGreaterThan = BuiltInValidationErrorDefinitions.GreaterThan(cache, 18);
        var secondGreaterThan = BuiltInValidationErrorDefinitions.GreaterThan(cache, 18);
        var firstIn = BuiltInValidationErrorDefinitions.IsIn(cache, 18, 65);
        var secondIn = BuiltInValidationErrorDefinitions.IsIn(cache, 18, 65);

        firstGreaterThan.Should().BeSameAs(secondGreaterThan);
        firstIn.Should().BeSameAs(secondIn);
    }

    [Fact]
    public void CachedDefinitions_ShouldUseActiveTemplatesAcrossValidationRuns()
    {
        var sharedCache = new ValidationErrorDefinitionCache();
        var firstContext = CreateContext(
            sharedCache,
            ValidationErrorTemplates.Default with
            {
                GreaterThan = new PrefixComparableTemplate("First run: ")
            }
        );
        var secondContext = CreateContext(
            sharedCache,
            ValidationErrorTemplates.Default with
            {
                GreaterThan = new PrefixComparableTemplate("Second run: ")
            }
        );
        var definition = BuiltInValidationErrorDefinitions.GreaterThan(firstContext.ErrorDefinitionCache, 18);

        firstContext.Check(10, target: "age", displayName: "Age").AddError(definition);
        secondContext.Check(10, target: "age", displayName: "Age").AddError(definition);

        firstContext.ToErrors()[0].Message.Should().Be("First run: Age > 18");
        secondContext.ToErrors()[0].Message.Should().Be("Second run: Age > 18");
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

    private sealed class PrefixComparableTemplate : IComparableValidationErrorMessageTemplate
    {
        private readonly string _prefix;

        public PrefixComparableTemplate(string prefix) => _prefix = prefix;

        public ValidationErrorMessage ProvideMessage<T, TParameter>(
            in ValidationErrorMessageContext<T> context,
            TParameter parameter
        ) =>
            new (_prefix + context.DisplayName + " > " + parameter);
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
            new (context.DisplayName + " must be divisible by " + Parameter);
    }
}
