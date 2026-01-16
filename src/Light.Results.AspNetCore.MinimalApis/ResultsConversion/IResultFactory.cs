using System;
using Light.Results.AspNetCore.Shared;
using Microsoft.AspNetCore.Http;

namespace Light.Results.AspNetCore.MinimalApis.ResultsConversion;

public interface IResultFactory
{
    IResult ToMinimalApiResult<T>(
        Result<T> result,
        Func<Result<T>, IResult>? onSuccess = null,
        string? instance = null,
        bool? firstCategoryIsLeadingCategory = null,
        ErrorSerializationFormat? errorFormat = null,
        HttpContext? httpContext = null
    );

    IResult ToMinimalApiResult(
        Result result,
        Func<Result, IResult>? onSuccess = null,
        string? instance = null,
        bool? firstCategoryIsLeadingCategory = null,
        ErrorSerializationFormat? errorFormat = null,
        HttpContext? httpContext = null
    );
}
