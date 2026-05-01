using System;
using Light.PortableResults.AspNetCore.OpenApi;
using Light.PortableResults.Validation.Definitions;
using Xunit;

namespace Light.PortableResults.Validation.OpenApi.Tests;

internal static class ValidationTypedHelperTestData
{
    public static TheoryData<string, string, string[]> TypedHelperCases =>
        new ()
        {
            { "EqualTo", ValidationErrorCodes.EqualTo, [ValidationErrorMetadataKeys.ComparativeValue] },
            { "NotEqualTo", ValidationErrorCodes.NotEqualTo, [ValidationErrorMetadataKeys.ComparativeValue] },
            { "GreaterThan", ValidationErrorCodes.GreaterThan, [ValidationErrorMetadataKeys.ComparativeValue] },
            {
                "GreaterThanOrEqualTo",
                ValidationErrorCodes.GreaterThanOrEqualTo,
                [ValidationErrorMetadataKeys.ComparativeValue]
            },
            { "LessThan", ValidationErrorCodes.LessThan, [ValidationErrorMetadataKeys.ComparativeValue] },
            {
                "LessThanOrEqualTo",
                ValidationErrorCodes.LessThanOrEqualTo,
                [ValidationErrorMetadataKeys.ComparativeValue]
            },
            {
                "InRange",
                ValidationErrorCodes.InRange,
                [ValidationErrorMetadataKeys.LowerBoundary, ValidationErrorMetadataKeys.UpperBoundary]
            },
            {
                "NotInRange",
                ValidationErrorCodes.NotInRange,
                [ValidationErrorMetadataKeys.LowerBoundary, ValidationErrorMetadataKeys.UpperBoundary]
            },
            {
                "ExclusiveRange",
                ValidationErrorCodes.ExclusiveRange,
                [ValidationErrorMetadataKeys.LowerBoundary, ValidationErrorMetadataKeys.UpperBoundary]
            }
        };

    internal static void AddTypedHelper<T>(string operationName, PortableProblemOpenApiBuilder builder)
    {
        switch (operationName)
        {
            case "EqualTo":
                builder.WithEqualToError<T>();
                break;
            case "NotEqualTo":
                builder.WithNotEqualToError<T>();
                break;
            case "GreaterThan":
                builder.WithGreaterThanError<T>();
                break;
            case "GreaterThanOrEqualTo":
                builder.WithGreaterThanOrEqualToError<T>();
                break;
            case "LessThan":
                builder.WithLessThanError<T>();
                break;
            case "LessThanOrEqualTo":
                builder.WithLessThanOrEqualToError<T>();
                break;
            case "InRange":
            case "InRangeDateTimeProblem":
                builder.WithInRangeError<T>();
                break;
            case "NotInRange":
                builder.WithNotInRangeError<T>();
                break;
            case "ExclusiveRange":
                builder.WithExclusiveRangeError<T>();
                break;
            default:
                throw new InvalidOperationException("Unknown helper: " + operationName);
        }
    }

    internal static void AddTypedHelper<T>(string operationName, PortableValidationProblemOpenApiBuilder builder)
    {
        switch (operationName)
        {
            case "EqualTo":
                builder.WithEqualToError<T>();
                break;
            case "NotEqualTo":
                builder.WithNotEqualToError<T>();
                break;
            case "GreaterThan":
                builder.WithGreaterThanError<T>();
                break;
            case "GreaterThanOrEqualTo":
                builder.WithGreaterThanOrEqualToError<T>();
                break;
            case "LessThan":
                builder.WithLessThanError<T>();
                break;
            case "LessThanOrEqualTo":
                builder.WithLessThanOrEqualToError<T>();
                break;
            case "InRange":
            case "InRangeDateTimeValidation":
                builder.WithInRangeError<T>();
                break;
            case "NotInRange":
                builder.WithNotInRangeError<T>();
                break;
            case "ExclusiveRange":
                builder.WithExclusiveRangeError<T>();
                break;
            default:
                throw new InvalidOperationException("Unknown helper: " + operationName);
        }
    }
}
