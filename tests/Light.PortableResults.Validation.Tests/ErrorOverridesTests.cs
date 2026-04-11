using System;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using FluentAssertions;
using Light.PortableResults.Metadata;
using Light.PortableResults.Validation.Definitions;
using Xunit;

namespace Light.PortableResults.Validation.Tests;

public sealed class ErrorOverridesTests
{
    [Fact]
    public void ValidationErrorOverrides_ShouldExposeExpectedShapeAndImplicitMessageConversion()
    {
        var properties = typeof(ErrorOverrides)
           .GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly)
           .OrderBy(property => property.Name, StringComparer.Ordinal)
           .Select(property => (property.Name, property.PropertyType))
           .ToArray();

        typeof(ErrorOverrides).IsValueType.Should().BeTrue();
        properties.Should().Equal(
            ("Category", typeof(ErrorCategory?)),
            ("Code", typeof(string)),
            ("Message", typeof(string)),
            ("Metadata", typeof(MetadataObject?))
        );

        ErrorOverrides overrides = "Comment must be present";
        overrides.Should().Be(new ErrorOverrides { Message = "Comment must be present" });
    }

    [Fact]
    public void MessageOnlyOverrides_ShouldSupportRepresentativeBuiltInAssertionFamilies()
    {
        var context = new DefaultValidationContextFactory().CreateValidationContext();

        context
           .Check(string.Empty, target: "comment", displayName: "Comment")
           .IsNotNullOrWhiteSpace("Comment must be present");
        context
           .Check("AB12", target: "code", displayName: "Code")
           .Matches("^[0-9]+$", "Code must contain only digits");
        context
           .Check("ab12", target: "alternateCode", displayName: "Alternate code")
           .Matches("^[A-Z]+$", "Alternate code is invalid", RegexOptions.IgnoreCase);
        context
           .Check<string?>("PendingApproval", target: "statusName", displayName: "Status name")
           .IsEnumName<OrderStatus>("Status name is invalid");
        context
           .Check(123.4500m, target: "amount", displayName: "Amount")
           .HasPrecisionAndScale(4, 2, "Amount format is invalid");
        context
           .Check("abc", target: "normalizedCode", displayName: "Normalized code")
           .IsEqualTo("ABC", StringComparer.Ordinal, "Normalized code must match ABC");

        context.Errors.Should().Equal(
            new Errors(
                new[]
                {
                    new Error
                    {
                        Message = "Comment must be present",
                        Code = "NotNullOrWhiteSpace",
                        Target = "comment",
                        Category = ErrorCategory.Validation
                    },
                    new Error
                    {
                        Message = "Code must contain only digits",
                        Code = "Matches",
                        Target = "code",
                        Category = ErrorCategory.Validation,
                        Metadata = MetadataObject.Create(
                            (ValidationErrorMetadataKeys.Pattern, "^[0-9]+$"),
                            (ValidationErrorMetadataKeys.RegexOptions, (int) RegexOptions.None)
                        )
                    },
                    new Error
                    {
                        Message = "Alternate code is invalid",
                        Code = "Matches",
                        Target = "alternateCode",
                        Category = ErrorCategory.Validation,
                        Metadata = MetadataObject.Create(
                            (ValidationErrorMetadataKeys.Pattern, "^[A-Z]+$"),
                            (ValidationErrorMetadataKeys.RegexOptions, (int) RegexOptions.IgnoreCase)
                        )
                    },
                    new Error
                    {
                        Message = "Status name is invalid",
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
                        Message = "Amount format is invalid",
                        Code = "PrecisionScale",
                        Target = "amount",
                        Category = ErrorCategory.Validation,
                        Metadata = MetadataObject.Create(
                            (ValidationErrorMetadataKeys.ExpectedPrecision, 4),
                            (ValidationErrorMetadataKeys.ExpectedScale, 2),
                            (ValidationErrorMetadataKeys.IgnoreTrailingZeros, false)
                        )
                    },
                    new Error
                    {
                        Message = "Normalized code must match ABC",
                        Code = "EqualTo",
                        Target = "normalizedCode",
                        Category = ErrorCategory.Validation,
                        Metadata = MetadataObject.Create(
                            (ValidationErrorMetadataKeys.ComparativeValue, "ABC")
                        )
                    }
                }
            )
        );
    }

    [Fact]
    public void NonMessageAndCombinedOverrides_ShouldPreserveDefaultsAndApplyExplicitOverrides()
    {
        var customMetadata = MetadataObject.Create(("Source", "Override"));
        var context = new DefaultValidationContextFactory().CreateValidationContext();

        context
           .Check(string.Empty, target: "comment", displayName: "Comment")
           .IsNotNullOrWhiteSpace(new ErrorOverrides { Code = "CommentRequired" });
        context
           .Check("abc", target: "status", displayName: "Status")
           .IsEqualTo("ABC", new ErrorOverrides { Category = ErrorCategory.UnprocessableContent });
        context
           .Check<string?>("AB", target: "tags", displayName: "Tags")
           .HasCount(3, new ErrorOverrides { Metadata = customMetadata });
        context
           .Check(string.Empty, target: "note", displayName: "Note")
           .IsNotNullOrWhiteSpace(
                new ErrorOverrides
                {
                    Message = "Note must be present",
                    Code = "NoteRequired",
                    Category = ErrorCategory.UnprocessableContent
                }
            );

        context.Errors.Should().Equal(
            new Errors(
                new[]
                {
                    new Error
                    {
                        Message = "Comment must not be empty or whitespace",
                        Code = "CommentRequired",
                        Target = "comment",
                        Category = ErrorCategory.Validation
                    },
                    new Error
                    {
                        Message = "Status must be equal to ABC",
                        Code = "EqualTo",
                        Target = "status",
                        Category = ErrorCategory.UnprocessableContent,
                        Metadata = MetadataObject.Create(
                            (ValidationErrorMetadataKeys.ComparativeValue, "ABC")
                        )
                    },
                    new Error
                    {
                        Message = "Tags must contain exactly 3 item(s)",
                        Code = "Count",
                        Target = "tags",
                        Category = ErrorCategory.Validation,
                        Metadata = customMetadata
                    },
                    new Error
                    {
                        Message = "Note must be present",
                        Code = "NoteRequired",
                        Target = "note",
                        Category = ErrorCategory.UnprocessableContent
                    }
                }
            )
        );
    }

    [Fact]
    public void Overrides_ShouldNotChangeScopedTargetResolution()
    {
        var context = new DefaultValidationContextFactory().CreateValidationContext();

        context.ForMember("address", isNormalized: true)
           .Check("A!", target: "zipCode", displayName: "Zip code")
           .ContainsOnlyLettersAndDigits(new ErrorOverrides { Code = "ZipCodeInvalid" });
        context.ForMember("contacts", isNormalized: true)
           .ForIndex(1)
           .Check("12A", target: "phone", displayName: "Phone")
           .ContainsOnlyDigits("Phone is invalid");

        context.Errors.Should().Equal(
            new Errors(
                new[]
                {
                    new Error
                    {
                        Message = "Zip code must contain only letters and digits",
                        Code = "ZipCodeInvalid",
                        Target = "address.zipCode",
                        Category = ErrorCategory.Validation
                    },
                    new Error
                    {
                        Message = "Phone is invalid",
                        Code = "DigitsOnly",
                        Target = "contacts[1].phone",
                        Category = ErrorCategory.Validation
                    }
                }
            )
        );
    }

    [Fact]
    public void OverrideOverloads_ShouldThrowForEmptyOrWhitespaceOverridesBeforeShortCircuit()
    {
        var context = new DefaultValidationContextFactory().CreateValidationContext();
        var check = context.Check("abc", target: "code", displayName: "Code").ShortCircuit();

        Action defaultAct = () => check.IsEmail(default(ErrorOverrides));
        Action emptyAct = () => check.IsEmail(new ErrorOverrides());
        Action whitespaceAct = () => check.IsEmail(new ErrorOverrides { Message = " " });

        defaultAct.Should().Throw<ArgumentException>().WithParameterName("overrides");
        emptyAct.Should().Throw<ArgumentException>().WithParameterName("overrides");
        whitespaceAct.Should().Throw<ArgumentException>().WithParameterName("overrides");
        context.Errors.Should().BeEmpty();
    }

    private enum OrderStatus
    {
        Pending,
        Approved
    }
}
