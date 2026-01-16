using System;
using Light.Results.AspNetCore.Shared;
using Light.Results.AspNetCore.Shared.HttpContextInjection;
using Microsoft.AspNetCore.Http;

namespace Light.Results.AspNetCore.MinimalApis.ResultsConversion;

public sealed class DefaultResultFactory : IResultFactory, IInjectHttpContext
{
    private readonly ResultFactoryOptions _options;
    private HttpContext? _httpContext;

    public DefaultResultFactory(ResultFactoryOptions options) =>
        _options = options ?? throw new ArgumentNullException(nameof(options));

    public HttpContext HttpContext
    {
        set => _httpContext = value ?? throw new ArgumentNullException(nameof(HttpContext));
    }

    public IResult ToMinimalApiResult<T>(
        Result<T> result,
        Func<Result<T>, IResult>? onSuccess = null,
        string? instance = null,
        bool? firstCategoryIsLeadingCategory = null,
        ErrorSerializationFormat? errorFormat = null,
        HttpContext? httpContext = null
    )
    {
        firstCategoryIsLeadingCategory ??= _options.FirstCategoryIsLeadingCategory;
        errorFormat ??= _options.ErrorSerializationFormat;
        httpContext ??= _httpContext;

        if (onSuccess is null)
        {
            return result.ToMinimalApiResult(
                httpContext,
                firstCategoryIsLeadingCategory.Value,
                instance,
                errorFormat.Value
            );
        }

        return result.ToMinimalApiResult(
            onSuccess,
            httpContext,
            firstCategoryIsLeadingCategory.Value,
            instance,
            errorFormat.Value
        );
    }

    public IResult ToMinimalApiResult(
        Result result,
        Func<Result, IResult>? onSuccess = null,
        string? instance = null,
        bool? firstCategoryIsLeadingCategory = null,
        ErrorSerializationFormat? errorFormat = null,
        HttpContext? httpContext = null
    )
    {
        firstCategoryIsLeadingCategory ??= _options.FirstCategoryIsLeadingCategory;
        errorFormat ??= _options.ErrorSerializationFormat;
        httpContext ??= _httpContext;

        if (onSuccess is null)
        {
            return result.ToMinimalApiResult(
                httpContext,
                firstCategoryIsLeadingCategory.Value,
                instance,
                errorFormat.Value
            );
        }

        return result.ToMinimalApiResult(
            onSuccess,
            httpContext,
            firstCategoryIsLeadingCategory.Value,
            instance,
            errorFormat.Value
        );
    }
}
