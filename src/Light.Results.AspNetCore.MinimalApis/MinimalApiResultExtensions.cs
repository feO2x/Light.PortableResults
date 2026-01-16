using System;
using System.Collections.Generic;
using Light.Results.AspNetCore.Abstractions;
using Light.Results.Metadata;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;

// ReSharper disable ConvertToExtensionBlock

namespace Light.Results.AspNetCore.MinimalApis;

/// <summary>
/// Extension methods for converting <see cref="Result{T}" /> and <see cref="Result" /> to ASP.NET Core Minimal API results.
/// </summary>
public static class MinimalApiResultExtensions
{
    /// <summary>
    /// Converts a Result&lt;T&gt; to an ASP.NET Core Minimal API IResult.
    /// On success, returns the value with HTTP 200.
    /// On failure, returns LightProblemDetails with appropriate HTTP status.
    /// </summary>
    /// <param name="result">The result to convert.</param>
    /// <param name="httpContext">HTTP context for enricher resolution (optional if no enricher registered).</param>
    /// <param name="firstCategoryIsLeadingCategory">If true, uses first error's category for status code.</param>
    /// <param name="instance">Optional URI identifying the specific occurrence.</param>
    /// <param name="errorFormat">Error serialization format (default: ASP.NET Core-compatible).</param>
    /// <typeparam name="T">The type of the result value.</typeparam>
    /// <returns>An IResult representing the result.</returns>
    public static IResult ToMinimalApiResult<T>(
        this Result<T> result,
        HttpContext? httpContext = null,
        bool firstCategoryIsLeadingCategory = false,
        string? instance = null,
        ErrorSerializationFormat errorFormat = ErrorSerializationFormat.AspNetCoreCompatible
    )
    {
        if (result.IsValid)
        {
            return TypedResults.Ok(result.Value);
        }

        var enrichedResult = EnrichIfRegistered(result, httpContext);
        return new LightProblemDetails(
            enrichedResult.Errors,
            enrichedResult.Metadata,
            firstCategoryIsLeadingCategory,
            instance,
            errorFormat
        );
    }

    /// <summary>
    /// Converts a Result&lt;T&gt; to an ASP.NET Core Minimal API IResult.
    /// On success, invokes the success factory to create the response.
    /// On failure, returns LightProblemDetails with appropriate HTTP status.
    /// </summary>
    /// <param name="result">The result to convert.</param>
    /// <param name="onSuccess">Factory function to create the success response.</param>
    /// <param name="httpContext">HTTP context for enricher resolution (optional if no enricher registered).</param>
    /// <param name="firstCategoryIsLeadingCategory">If true, uses first error's category for status code.</param>
    /// <param name="instance">Optional URI identifying the specific occurrence.</param>
    /// <param name="errorFormat">Error serialization format (default: ASP.NET Core-compatible).</param>
    /// <typeparam name="T">The type of the result value.</typeparam>
    /// <returns>An IResult representing the result.</returns>
    public static IResult ToMinimalApiResult<T>(
        this Result<T> result,
        Func<T, IResult> onSuccess,
        HttpContext? httpContext = null,
        bool firstCategoryIsLeadingCategory = false,
        string? instance = null,
        ErrorSerializationFormat errorFormat = ErrorSerializationFormat.AspNetCoreCompatible
    )
    {
        ArgumentNullException.ThrowIfNull(onSuccess);

        if (result.IsValid)
        {
            return onSuccess(result.Value);
        }

        var enrichedResult = EnrichIfRegistered(result, httpContext);
        return new LightProblemDetails(
            enrichedResult.Errors,
            enrichedResult.Metadata,
            firstCategoryIsLeadingCategory,
            instance,
            errorFormat
        );
    }

    /// <summary>
    /// Converts a Result to an ASP.NET Core Minimal API IResult.
    /// On success, returns HTTP 204 No Content.
    /// On failure, returns LightProblemDetails with appropriate HTTP status.
    /// </summary>
    /// <param name="result">The result to convert.</param>
    /// <param name="httpContext">HTTP context for enricher resolution (optional if no enricher registered).</param>
    /// <param name="firstCategoryIsLeadingCategory">If true, uses first error's category for status code.</param>
    /// <param name="instance">Optional URI identifying the specific occurrence.</param>
    /// <param name="errorFormat">Error serialization format (default: ASP.NET Core-compatible).</param>
    /// <returns>An IResult representing the result.</returns>
    public static IResult ToMinimalApiResult(
        this Result result,
        HttpContext? httpContext = null,
        bool firstCategoryIsLeadingCategory = false,
        string? instance = null,
        ErrorSerializationFormat errorFormat = ErrorSerializationFormat.AspNetCoreCompatible
    )
    {
        if (result.IsValid)
        {
            return TypedResults.NoContent();
        }

        var enrichedResult = EnrichIfRegistered(result, httpContext);
        return new LightProblemDetails(
            enrichedResult.Errors,
            enrichedResult.Metadata,
            firstCategoryIsLeadingCategory,
            instance,
            errorFormat
        );
    }

    /// <summary>
    /// Converts a Result to an ASP.NET Core Minimal API IResult.
    /// On success, invokes the success factory to create the response.
    /// On failure, returns LightProblemDetails with appropriate HTTP status.
    /// </summary>
    /// <param name="result">The result to convert.</param>
    /// <param name="onSuccess">Factory function to create the success response.</param>
    /// <param name="httpContext">HTTP context for enricher resolution (optional if no enricher registered).</param>
    /// <param name="firstCategoryIsLeadingCategory">If true, uses first error's category for status code.</param>
    /// <param name="instance">Optional URI identifying the specific occurrence.</param>
    /// <param name="errorFormat">Error serialization format (default: ASP.NET Core-compatible).</param>
    /// <returns>An IResult representing the result.</returns>
    public static IResult ToMinimalApiResult(
        this Result result,
        Func<IResult> onSuccess,
        HttpContext? httpContext = null,
        bool firstCategoryIsLeadingCategory = false,
        string? instance = null,
        ErrorSerializationFormat errorFormat = ErrorSerializationFormat.AspNetCoreCompatible
    )
    {
        ArgumentNullException.ThrowIfNull(onSuccess);

        if (result.IsValid)
        {
            return onSuccess();
        }

        var enrichedResult = EnrichIfRegistered(result, httpContext);
        return new LightProblemDetails(
            enrichedResult.Errors,
            enrichedResult.Metadata,
            firstCategoryIsLeadingCategory,
            instance,
            errorFormat
        );
    }

    /// <summary>
    /// Converts a failed Result to ASP.NET Core's ProblemDetails.
    /// Throws if the result is valid.
    /// </summary>
    /// <param name="result">The failed result to convert.</param>
    /// <param name="firstCategoryIsLeadingCategory">If true, uses first error's category for status code.</param>
    /// <returns>An ASP.NET Core ProblemDetails instance.</returns>
    /// <exception cref="InvalidOperationException">Thrown when the result is valid.</exception>
    public static ProblemDetails ToProblemDetails(
        this Result result,
        bool firstCategoryIsLeadingCategory = false
    )
    {
        if (result.IsValid)
        {
            throw new InvalidOperationException("Cannot convert a successful result to ProblemDetails.");
        }

        return CreateProblemDetails(
            result.Errors,
            result.Metadata,
            firstCategoryIsLeadingCategory
        );
    }

    /// <summary>
    /// Converts a failed Result&lt;T&gt; to ASP.NET Core's ProblemDetails.
    /// Throws if the result is valid.
    /// </summary>
    /// <param name="result">The failed result to convert.</param>
    /// <param name="firstCategoryIsLeadingCategory">If true, uses first error's category for status code.</param>
    /// <typeparam name="T">The type of the result value.</typeparam>
    /// <returns>An ASP.NET Core ProblemDetails instance.</returns>
    /// <exception cref="InvalidOperationException">Thrown when the result is valid.</exception>
    public static ProblemDetails ToProblemDetails<T>(
        this Result<T> result,
        bool firstCategoryIsLeadingCategory = false
    )
    {
        if (result.IsValid)
        {
            throw new InvalidOperationException("Cannot convert a successful result to ProblemDetails.");
        }

        return CreateProblemDetails(
            result.Errors,
            result.Metadata,
            firstCategoryIsLeadingCategory
        );
    }

    private static Result<T> EnrichIfRegistered<T>(Result<T> result, HttpContext? httpContext)
    {
        if (httpContext is null)
        {
            return result;
        }

        var enricher = httpContext.RequestServices.GetService<IHttpResultEnricher>();
        return enricher?.Enrich(result, httpContext) ?? result;
    }

    private static Result EnrichIfRegistered(Result result, HttpContext? httpContext)
    {
        if (httpContext is null)
        {
            return result;
        }

        var enricher = httpContext.RequestServices.GetService<IHttpResultEnricher>();
        return enricher?.Enrich(result, httpContext) ?? result;
    }

    private static ProblemDetails CreateProblemDetails(
        Errors errors,
        MetadataObject? metadata,
        bool firstCategoryIsLeadingCategory
    )
    {
        var leadingCategory = errors.GetLeadingCategory(firstCategoryIsLeadingCategory);
        var statusCode = leadingCategory.ToHttpStatusCode();

        var pd = new ProblemDetails
        {
            Type = HttpStatusCodeInfo.GetTypeUri(statusCode),
            Title = HttpStatusCodeInfo.GetTitle(statusCode),
            Status = statusCode,
            Detail = errors.First.Message
        };

        var errorList = new List<object>(errors.Count);
        foreach (var error in errors)
        {
            errorList.Add(
                new Dictionary<string, object?>
                {
                    ["message"] = error.Message,
                    ["code"] = error.Code,
                    ["target"] = error.Target,
                    ["category"] = error.Category == ErrorCategory.Unclassified ? null : error.Category.ToString(),
                    ["metadata"] = error.Metadata.HasValue ? ConvertMetadataToDict(error.Metadata.Value) : null
                }
            );
        }

        pd.Extensions["errors"] = errorList;

        if (metadata.HasValue)
        {
            foreach (var kvp in metadata.Value)
            {
                pd.Extensions[kvp.Key] = ConvertMetadataValue(kvp.Value);
            }
        }

        return pd;
    }

    private static Dictionary<string, object?> ConvertMetadataToDict(MetadataObject metadata)
    {
        var dict = new Dictionary<string, object?>(metadata.Count);
        foreach (var kvp in metadata)
        {
            dict[kvp.Key] = ConvertMetadataValue(kvp.Value);
        }

        return dict;
    }

    private static object? ConvertMetadataValue(MetadataValue value)
    {
        switch (value.Kind)
        {
            case MetadataKind.Null:
                return null;
            case MetadataKind.Boolean:
                value.TryGetBoolean(out var boolVal);
                return boolVal;
            case MetadataKind.Int64:
                value.TryGetInt64(out var longVal);
                return longVal;
            case MetadataKind.Double:
                value.TryGetDouble(out var doubleVal);
                return doubleVal;
            case MetadataKind.String:
                value.TryGetString(out var stringVal);
                return stringVal;
            case MetadataKind.Array:
                value.TryGetArray(out var arrayVal);
                var list = new List<object?>(arrayVal.Count);
                foreach (var item in arrayVal)
                {
                    list.Add(ConvertMetadataValue(item));
                }

                return list;
            case MetadataKind.Object:
                value.TryGetObject(out var objVal);
                return ConvertMetadataToDict(objVal);
            default:
                return null;
        }
    }
}
