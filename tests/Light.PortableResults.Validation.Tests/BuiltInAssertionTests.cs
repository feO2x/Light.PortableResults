using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Text.RegularExpressions;
using FluentAssertions;
using Light.PortableResults.Metadata;
using Xunit;

namespace Light.PortableResults.Validation.Tests;

public sealed class BuiltInAssertionTests
{
    [Fact]
    public void IsNotNullAndIsNull_ShouldHonorDefaultGuardSemantics()
    {
        string? nullableValue = null;
        var context = new DefaultValidationContextFactory().CreateValidationContext();

        context.Check("abc", NoOpValueNormalizer.Instance, target: "code", displayName: "Code").IsNull();
        context
           .Check(nullableValue, NoOpValueNormalizer.Instance, target: "name", displayName: "Name")
           .IsNotNull();

        context.Errors.Should().Equal(
            new Errors(
                new[]
                {
                    new Error
                    {
                        Message = "Code must be null",
                        Code = "Null",
                        Target = "code",
                        Category = ErrorCategory.Validation
                    },
                    new Error
                    {
                        Message = "Name must not be null",
                        Code = "NotNull",
                        Target = "name",
                        Category = ErrorCategory.Validation
                    }
                }
            )
        );
    }

    [Fact]
    public void EqualityAssertions_ShouldUseComparersAndMetadata()
    {
        var context = new DefaultValidationContextFactory().CreateValidationContext();

        context.Check("abc", target: "code", displayName: "Code")
           .IsEqualTo("ABC", StringComparer.OrdinalIgnoreCase)
           .IsNotEqualTo("ABC", StringComparer.OrdinalIgnoreCase);

        context.Errors.Should().Equal(
            new Errors(
                new Error
                {
                    Message = "Code must not be equal to ABC",
                    Code = "NotEqualTo",
                    Target = "code",
                    Category = ErrorCategory.Validation,
                    Metadata = MetadataObject.Create(
                        (ValidationErrorMetadataKeys.ComparativeValue, "ABC")
                    )
                }
            )
        );
    }

    [Fact]
    public void IsEmptyAndIsNotEmpty_ShouldSupportStringGuidAndCollectionSemantics()
    {
        var displayName = " ";
        IEnumerable<int>? items = null;
        var context = new DefaultValidationContextFactory().CreateValidationContext();

        context
           .Check<string?>(
                displayName,
                NoOpValueNormalizer.Instance,
                displayName: "Display name"
            )
           .IsEmpty();
        context.Check(Guid.Empty, target: "id", displayName: "Id").IsNotEmpty();
        context.Check(items!, displayName: "Items").IsNotEmpty();

        context.Errors.Should().Equal(
            new Errors(
                new[]
                {
                    new Error
                    {
                        Message = "Display name must be empty",
                        Code = "Empty",
                        Target = "displayName",
                        Category = ErrorCategory.Validation
                    },
                    new Error
                    {
                        Message = "Id must not be empty",
                        Code = "NotEmpty",
                        Target = "id",
                        Category = ErrorCategory.Validation
                    },
                    new Error
                    {
                        Message = "Items must not be empty",
                        Code = "NotEmpty",
                        Target = "items",
                        Category = ErrorCategory.Validation
                    }
                }
            )
        );
    }

    [Fact]
    public void ComparableAssertions_ShouldThrowOnNullUnlessTheCheckWasShortCircuited()
    {
        int? age = null;
        var options = ValidationContextOptions.Default with
        {
            AutomaticNullErrorProvider = NoOpAutomaticNullErrorProvider.Instance
        };
        var context = new DefaultValidationContextFactory(options).CreateValidationContext();

        Action throwingAct = () => context.Check(age, displayName: "Age").IsGreaterThan(18);
        Action shortCircuitedAct =
            () => context.Check(age, displayName: "Age").IsNotNull().IsGreaterThan(18);

        throwingAct.Should().Throw<InvalidOperationException>()
           .WithMessage("*IsGreaterThan*IsNotNull()*");
        shortCircuitedAct.Should().NotThrow();
    }

    [Fact]
    public void ComparableAssertions_ShouldAddExpectedErrorsAndMetadata()
    {
        var context = new DefaultValidationContextFactory().CreateValidationContext();

        context.Check(10, target: "age", displayName: "Age")
           .IsGreaterThanOrEqualTo(18)
           .IsLessThanOrEqualTo(5)
           .IsNotIn(1, 10)
           .IsInExclusiveRange(10, 20);

        context.Errors.Should().Equal(
            new Errors(
                new[]
                {
                    new Error
                    {
                        Message = "Age must be greater than or equal to 18",
                        Code = "GreaterThanOrEqualTo",
                        Target = "age",
                        Category = ErrorCategory.Validation,
                        Metadata = MetadataObject.Create(
                            (ValidationErrorMetadataKeys.ComparativeValue, 18)
                        )
                    },
                    new Error
                    {
                        Message = "Age must be less than or equal to 5",
                        Code = "LessThanOrEqualTo",
                        Target = "age",
                        Category = ErrorCategory.Validation,
                        Metadata = MetadataObject.Create(
                            (ValidationErrorMetadataKeys.ComparativeValue, 5)
                        )
                    },
                    new Error
                    {
                        Message = "Age must not be between 1 and 10",
                        Code = "NotIn",
                        Target = "age",
                        Category = ErrorCategory.Validation,
                        Metadata = MetadataObject.Create(
                            (ValidationErrorMetadataKeys.LowerBoundary, 1),
                            (ValidationErrorMetadataKeys.UpperBoundary, 10)
                        )
                    },
                    new Error
                    {
                        Message = "Age must be between 10 and 20 (exclusive)",
                        Code = "ExclusiveRange",
                        Target = "age",
                        Category = ErrorCategory.Validation,
                        Metadata = MetadataObject.Create(
                            (ValidationErrorMetadataKeys.LowerBoundary, 10),
                            (ValidationErrorMetadataKeys.UpperBoundary, 20)
                        )
                    }
                }
            )
        );
    }

    [Fact]
    public void StringAssertions_ShouldRespectNormalizationAndThrowForNullFollowUpChecks()
    {
        string nullableValue = null!;
        var defaultContext = new DefaultValidationContextFactory().CreateValidationContext();
        var noOpContext = new DefaultValidationContextFactory(
            ValidationContextOptions.Default with { ValueNormalizer = NoOpValueNormalizer.Instance }
        ).CreateValidationContext();

        defaultContext.Check(nullableValue, target: "name", displayName: "Name").IsNotNullOrWhiteSpace();

        Action act = () => noOpContext.Check(
                nullableValue,
                NoOpValueNormalizer.Instance,
                target: "code",
                displayName: "Code"
            )
           .HasMinLength(2);

        defaultContext.Errors.Should().Equal(
            new Errors(
                new Error
                {
                    Message = "Name must not be empty or whitespace",
                    Code = "NotNullOrWhiteSpace",
                    Target = "name",
                    Category = ErrorCategory.Validation
                }
            )
        );
        act.Should().Throw<InvalidOperationException>()
           .WithMessage("*HasMinLength*string normalization*IsNotNull()*");
    }

    [Fact]
    public void StringAssertions_ShouldComposeTargetsAcrossRootMemberAndIndexedScopes()
    {
        var context = new DefaultValidationContextFactory().CreateValidationContext();
        var childContext = context.ForMember("address", isNormalized: true);
        var indexedContext = context.ForMember("contacts", isNormalized: true).ForIndex(1);
        var email = "abc@";
        var zipCode = "A!";
        var phone = "12A";

        context.Check(email, displayName: "Email").IsEmail();
        childContext.Check(zipCode, displayName: "Zip code").ContainsOnlyLettersAndDigits();
        indexedContext.Check(phone, displayName: "Phone").ContainsOnlyDigits();

        context.Errors.Should().Equal(
            new Errors(
                new[]
                {
                    new Error
                    {
                        Message = "Email must be an email address",
                        Code = "Email",
                        Target = "email",
                        Category = ErrorCategory.Validation
                    },
                    new Error
                    {
                        Message = "Zip code must contain only letters and digits",
                        Code = "LettersAndDigitsOnly",
                        Target = "address.zipCode",
                        Category = ErrorCategory.Validation
                    },
                    new Error
                    {
                        Message = "Phone must contain only digits",
                        Code = "DigitsOnly",
                        Target = "contacts[1].phone",
                        Category = ErrorCategory.Validation
                    }
                }
            )
        );
    }

    [Fact]
    public void Matches_ShouldUseCachedDefinitionMetadataForPatternAndOptions()
    {
        var context = new DefaultValidationContextFactory().CreateValidationContext();
        var code = "AB12";

        context.Check(code, displayName: "Code")
           .Matches("^[0-9]+$", RegexOptions.IgnoreCase);

        context.Errors.Should().Equal(
            new Errors(
                new Error
                {
                    Message = "Code has an invalid format",
                    Code = "Matches",
                    Target = "code",
                    Category = ErrorCategory.Validation,
                    Metadata = MetadataObject.Create(
                        (ValidationErrorMetadataKeys.Pattern, "^[0-9]+$"),
                        (ValidationErrorMetadataKeys.RegexOptions, (int) RegexOptions.IgnoreCase)
                    )
                }
            )
        );
    }

    [Fact]
    public void CollectionAssertions_ShouldSupportStringsFallbackEnumerablesAndImmutableArrays()
    {
        var context = new DefaultValidationContextFactory().CreateValidationContext();
        var code = "AB";

        context.Check<string?>(code, displayName: "Code").HasCount(3);
        context.Check<IEnumerable<int>>(new CountingEnumerable(1, 2, 3), target: "items", displayName: "Items")
           .HasMinCount(4);
        context.Check(ImmutableArray.Create(1, 2), target: "tags", displayName: "Tags").HasMaxCount(1);

        context.Errors.Should().Equal(
            new Errors(
                new[]
                {
                    new Error
                    {
                        Message = "Code must contain exactly 3 item(s)",
                        Code = "Count",
                        Target = "code",
                        Category = ErrorCategory.Validation,
                        Metadata = MetadataObject.Create(
                            (ValidationErrorMetadataKeys.ExpectedCount, 3)
                        )
                    },
                    new Error
                    {
                        Message = "Items must contain at least 4 item(s)",
                        Code = "MinCount",
                        Target = "items",
                        Category = ErrorCategory.Validation,
                        Metadata = MetadataObject.Create(
                            (ValidationErrorMetadataKeys.MinCount, 4)
                        )
                    },
                    new Error
                    {
                        Message = "Tags must contain at most 1 item(s)",
                        Code = "MaxCount",
                        Target = "tags",
                        Category = ErrorCategory.Validation,
                        Metadata = MetadataObject.Create(
                            (ValidationErrorMetadataKeys.MaxCount, 1)
                        )
                    }
                }
            )
        );
    }

    [Fact]
    public void CollectionAssertions_ShouldThrowOnNullUnlessTheCheckWasShortCircuited()
    {
        IEnumerable<int> items = null!;
        var context = new DefaultValidationContextFactory().CreateValidationContext();

        Action throwingAct = () =>
            context.Check(items!, displayName: "Items").HasCount(1);
        Action shortCircuitedAct = () => context.Check(items, displayName: "Items")
           .IsNotNull()
           .HasCount(1);

        throwingAct.Should().Throw<InvalidOperationException>()
           .WithMessage("*HasCount*collection*IsNotNull()*");
        shortCircuitedAct.Should().NotThrow();
    }

    [Fact]
    public void EnumAndDecimalAssertions_ShouldGenerateExpectedErrorsAndMetadata()
    {
        var context = new DefaultValidationContextFactory().CreateValidationContext();
        var statusName = "PendingApproval";

        context.Check((OrderStatus) 99, target: "status", displayName: "Status").IsInEnum();
        context.Check<string?>(statusName, displayName: "Status name")
           .IsEnumName<OrderStatus>();
        context.Check(123.4500m, target: "amount", displayName: "Amount")
           .HasPrecisionAndScale(4, 2, ignoreTrailingZeros: true);

        context.Errors.Should().Equal(
            new Errors(
                new[]
                {
                    new Error
                    {
                        Message = "Status must be a defined enum value",
                        Code = "Enum",
                        Target = "status",
                        Category = ErrorCategory.Validation,
                        Metadata = MetadataObject.Create(
                            (ValidationErrorMetadataKeys.EnumType, typeof(OrderStatus).FullName!)
                        )
                    },
                    new Error
                    {
                        Message = "Status name must be a valid enum name",
                        Code = "EnumName",
                        Target = "statusName",
                        Category = ErrorCategory.Validation,
                        Metadata = MetadataObject.Create(
                            (ValidationErrorMetadataKeys.EnumType, typeof(OrderStatus).FullName!),
                            (ValidationErrorMetadataKeys.IgnoreCase, false)
                        )
                    },
                    new Error
                    {
                        Message =
                            "Amount must not be more than 4 digits in total, with allowance for 2 decimals when trailing decimal zeros are ignored",
                        Code = "PrecisionScale",
                        Target = "amount",
                        Category = ErrorCategory.Validation,
                        Metadata = MetadataObject.Create(
                            (ValidationErrorMetadataKeys.ExpectedPrecision, 4),
                            (ValidationErrorMetadataKeys.ExpectedScale, 2),
                            (ValidationErrorMetadataKeys.IgnoreTrailingZeros, true)
                        )
                    }
                }
            )
        );
    }

    [Fact]
    public void MustAndCustom_ShouldRespectShortCircuitingAndScopedTargets()
    {
        var context = new DefaultValidationContextFactory(
            ValidationContextOptions.Default with { ValueNormalizer = NoOpValueNormalizer.Instance }
        ).CreateValidationContext();
        string? nullableValue = null;
        var predicateInvocationCount = 0;

        context.Check(nullableValue, NoOpValueNormalizer.Instance, target: "name", displayName: "Name")
           .IsNotNull()
           .Must(
                value =>
                {
                    predicateInvocationCount++;
                    return value is not null;
                }
            );

        context.ForMember("customer", isNormalized: true)
           .Check("A", target: "code", displayName: "Code")
           .Must(value => value.Length > 2)
           .Custom(
                (customContext, value) =>
                {
                    customContext.AddError("Code contains unsupported data", "Custom");
                    customContext.AddError(
                        "Code detail is invalid",
                        "CustomDetail",
                        ValidationTarget.Relative("detail", isNormalized: true)
                    );
                }
            );

        predicateInvocationCount.Should().Be(0);
        context.Errors.Should().Equal(
            new Errors(
                new[]
                {
                    new Error
                    {
                        Message = "Name must not be null",
                        Code = "NotNull",
                        Target = "name",
                        Category = ErrorCategory.Validation
                    },
                    new Error
                    {
                        Message = "Code is invalid",
                        Code = "Predicate",
                        Target = "customer.code",
                        Category = ErrorCategory.Validation
                    },
                    new Error
                    {
                        Message = "Code contains unsupported data",
                        Code = "Custom",
                        Target = "customer.code",
                        Category = ErrorCategory.Validation
                    },
                    new Error
                    {
                        Message = "Code detail is invalid",
                        Code = "CustomDetail",
                        Target = "customer.code.detail",
                        Category = ErrorCategory.Validation
                    }
                }
            )
        );
    }

    private enum OrderStatus
    {
        Pending,
        Approved
    }

    private sealed class CountingEnumerable : IEnumerable<int>
    {
        private readonly IReadOnlyList<int> _values;

        public CountingEnumerable(params int[] values) => _values = values;

        public IEnumerator<int> GetEnumerator()
        {
            for (var i = 0; i < _values.Count; i++)
            {
                yield return _values[i];
            }
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}
