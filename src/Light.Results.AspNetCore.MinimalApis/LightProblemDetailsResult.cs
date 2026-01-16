using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;
using System.Threading.Tasks;
using Light.Results.AspNetCore.MinimalApis.Serialization;
using Light.Results.AspNetCore.Shared;
using Light.Results.Metadata;
using Microsoft.AspNetCore.Http;

namespace Light.Results.AspNetCore.MinimalApis;

/// <summary>
/// RFC 7807/9457-compliant Problem Details response that implements IResult directly.
/// Avoids the allocation overhead of ASP.NET Core's ProblemDetails.Extensions dictionary.
/// </summary>
[JsonConverter(typeof(LightProblemDetailsResultJsonConverter))]
public sealed class LightProblemDetailsResult : IResult
{
    /// <summary>
    /// Initializes a new instance of <see cref="LightProblemDetailsResult" />.
    /// </summary>
    /// <param name="errors">The errors from the failed result.</param>
    /// <param name="metadata">The result-level metadata.</param>
    /// <param name="firstCategoryIsLeadingCategory">If true, uses first error's category for status code.</param>
    /// <param name="instance">Optional URI identifying the specific occurrence.</param>
    /// <param name="errorFormat">Error serialization format.</param>
    /// <param name="serializerOptions">The serializer options to use for serializing the problem details.</param>
    public LightProblemDetailsResult(
        Errors errors,
        MetadataObject? metadata,
        bool firstCategoryIsLeadingCategory = false,
        string? instance = null,
        ErrorSerializationFormat errorFormat = ErrorSerializationFormat.AspNetCoreCompatible,
        JsonSerializerOptions? serializerOptions = null
    )
    {
        if (errors.IsDefaultInstance)
        {
            throw new ArgumentException($"The {nameof(errors)} argument must contain at least one error");
        }

        var leadingCategory = errors.GetLeadingCategory(firstCategoryIsLeadingCategory);
        Status = leadingCategory.ToHttpStatusCode();
        Type = HttpStatusCodeInfo.GetTypeUri(Status);
        Title = HttpStatusCodeInfo.GetTitle(Status);
        Detail = HttpStatusCodeInfo.GetDetail(leadingCategory);
        Instance = instance;
        Errors = errors;
        Metadata = metadata;
        ErrorFormat = errorFormat;
        SerializerOptions = serializerOptions ?? JsonDefaults.Options;
    }

    /// <summary>RFC 9110 section URI for the status code.</summary>
    public string Type { get; }

    /// <summary>HTTP status phrase.</summary>
    public string Title { get; }

    /// <summary>HTTP status code.</summary>
    public int Status { get; }

    /// <summary>Human-readable explanation based on the error category.</summary>
    public string Detail { get; }

    /// <summary>URI reference identifying the specific occurrence (optional).</summary>
    public string? Instance { get; }

    /// <summary>The errors from the failed result.</summary>
    public Errors Errors { get; }

    /// <summary>Result-level metadata (serialized as extension properties).</summary>
    public MetadataObject? Metadata { get; }

    /// <summary>The serialization format for errors.</summary>
    public ErrorSerializationFormat ErrorFormat { get; }

    /// <summary>
    /// Gets the serializer options used for serializing the problem details.
    /// </summary>
    public JsonSerializerOptions SerializerOptions { get; }

    /// <summary>
    /// Writes the Problem Details JSON directly to the HTTP response.
    /// Uses source-generated serialization for optimal performance.
    /// </summary>
    /// <param name="httpContext">The HTTP context.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public Task ExecuteAsync(HttpContext httpContext)
    {
        httpContext.Response.StatusCode = Status;
        httpContext.Response.ContentType = "application/problem+json";
        var typeInfo =
            (JsonTypeInfo<LightProblemDetailsResult>) SerializerOptions.GetTypeInfo(typeof(LightProblemDetailsResult));
        return JsonSerializer.SerializeAsync(
            httpContext.Response.Body,
            this,
            typeInfo,
            httpContext.RequestAborted
        );
    }
}
