using FluentAssertions;
using Light.PortableResults.Metadata;
using Light.PortableResults.Validation.Assertions;
using Xunit;

namespace Light.PortableResults.Validation.Tests;

public sealed class BuiltInAssertionTests
{
    [Fact]
    public void IsNotNull_ShouldAddErrorAndShortCircuitByDefault()
    {
        int? age = null;
        var context = new DefaultValidationContextFactory().CreateValidationContext();

        var check = context.Check(age, displayName: "Age").IsNotNull();
        check.IsGreaterThan(18);

        check.IsShortCircuited.Should().BeTrue();
        context.ToErrors().Should().Equal(
            new Errors(
                new Error
                {
                    Message = "Age must not be null",
                    Code = "NotNull",
                    Target = "age",
                    Category = ErrorCategory.Validation
                }
            )
        );
    }

    [Fact]
    public void IsNotNull_ShouldAllowOptingOutOfShortCircuiting()
    {
        int? age = null;
        var context = new DefaultValidationContextFactory().CreateValidationContext();

        var check = context.Check(age, displayName: "Age")
           .IsNotNull(shortCircuitOnError: false);

        check.IsShortCircuited.Should().BeFalse();
        context.ToErrors().Should().Equal(
            new Errors(
                new Error
                {
                    Message = "Age must not be null",
                    Code = "NotNull",
                    Target = "age",
                    Category = ErrorCategory.Validation
                }
            )
        );
    }

    [Fact]
    public void IsGreaterThan_AndIsLessThan_ShouldHonorDefaultShortCircuitBehavior()
    {
        var context = new DefaultValidationContextFactory().CreateValidationContext();

        var check = context.Check(10, target: "age", displayName: "Age")
           .IsGreaterThan(18)
           .IsLessThan(5);

        check.IsShortCircuited.Should().BeFalse();
        context.ToErrors().Should().Equal(
            new Errors(
                new[]
                {
                    new Error
                    {
                        Message = "Age must be greater than 18",
                        Code = "GreaterThan",
                        Target = "age",
                        Category = ErrorCategory.Validation,
                        Metadata = MetadataObject.Create(
                            (ValidationErrorMetadataKeys.ComparativeValue, 18)
                        )
                    },
                    new Error
                    {
                        Message = "Age must be less than 5",
                        Code = "LessThan",
                        Target = "age",
                        Category = ErrorCategory.Validation,
                        Metadata = MetadataObject.Create(
                            (ValidationErrorMetadataKeys.ComparativeValue, 5)
                        )
                    }
                }
            )
        );
    }

    [Fact]
    public void IsLessThan_ShouldAllowOptionalShortCircuiting()
    {
        var context = new DefaultValidationContextFactory().CreateValidationContext();

        var check = context.Check(10, target: "age", displayName: "Age")
           .IsLessThan(5, shortCircuitOnError: true)
           .IsGreaterThan(20);

        check.IsShortCircuited.Should().BeTrue();
        context.ToErrors().Should().Equal(
            new Errors(
                new Error
                {
                    Message = "Age must be less than 5",
                    Code = "LessThan",
                    Target = "age",
                    Category = ErrorCategory.Validation,
                    Metadata = MetadataObject.Create(
                        (ValidationErrorMetadataKeys.ComparativeValue, 5)
                    )
                }
            )
        );
    }

    [Fact]
    public void ComparisonAssertions_ShouldSkipNullValues()
    {
        int? age = null;
        var context = new DefaultValidationContextFactory().CreateValidationContext();

        var check = context.Check(age, displayName: "Age")
           .IsGreaterThan(18)
           .IsLessThan(65)
           .IsIn(18, 65);

        check.IsShortCircuited.Should().BeFalse();
        context.HasErrors.Should().BeFalse();
    }

    [Fact]
    public void IsIn_ShouldUseCachedDefinitionDefaults()
    {
        var context = new DefaultValidationContextFactory().CreateValidationContext();

        context.Check(70, target: "age", displayName: "Age").IsIn(18, 65);

        context.ToErrors().Should().Equal(
            new Errors(
                new Error
                {
                    Message = "Age must be between 18 and 65",
                    Code = "IsIn",
                    Target = "age",
                    Category = ErrorCategory.Validation,
                    Metadata = MetadataObject.Create(
                        (ValidationErrorMetadataKeys.LowerBoundary, 18),
                        (ValidationErrorMetadataKeys.UpperBoundary, 65)
                    )
                }
            )
        );
    }

    [Fact]
    public void BuiltInAssertions_ShouldComposeTargetsAcrossParentChildAndIndexedScopes()
    {
        var context = new DefaultValidationContextFactory().CreateValidationContext();
        var childContext = context.ForMember("address", isNormalized: true);
        var indexedContext = context.ForMember("addresses", isNormalized: true).ForIndex(2);

        context.Check(10, target: "age", displayName: "Age").IsGreaterThan(18);
        childContext.Check(5, target: "zipCode", displayName: "Zip code").IsIn(10000, 99999);
        indexedContext.Check(10, target: "zipCode", displayName: "Zip code").IsLessThan(5);

        context.ToErrors().Should().Equal(
            new Errors(
                new[]
                {
                    new Error
                    {
                        Message = "Age must be greater than 18",
                        Code = "GreaterThan",
                        Target = "age",
                        Category = ErrorCategory.Validation,
                        Metadata = MetadataObject.Create(
                            (ValidationErrorMetadataKeys.ComparativeValue, 18)
                        )
                    },
                    new Error
                    {
                        Message = "Zip code must be between 10000 and 99999",
                        Code = "IsIn",
                        Target = "address.zipCode",
                        Category = ErrorCategory.Validation,
                        Metadata = MetadataObject.Create(
                            (ValidationErrorMetadataKeys.LowerBoundary, 10000),
                            (ValidationErrorMetadataKeys.UpperBoundary, 99999)
                        )
                    },
                    new Error
                    {
                        Message = "Zip code must be less than 5",
                        Code = "LessThan",
                        Target = "addresses[2].zipCode",
                        Category = ErrorCategory.Validation,
                        Metadata = MetadataObject.Create(
                            (ValidationErrorMetadataKeys.ComparativeValue, 5)
                        )
                    }
                }
            )
        );
    }
}
