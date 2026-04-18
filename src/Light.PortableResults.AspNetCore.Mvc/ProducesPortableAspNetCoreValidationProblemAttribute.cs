using Light.PortableResults.AspNetCore.Shared;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Light.PortableResults.AspNetCore.Mvc;

/// <summary>
/// Specifies that the action produces an ASP.NET Core-compatible Light.PortableResults validation
/// problem details response with untyped metadata. Use this attribute when
/// <c>ValidationProblemSerializationFormat</c> is set to <c>AspNetCoreCompatible</c>. The response
/// type is documented as <see cref="PortableAspNetCoreValidationProblemDetails" />.
/// </summary>
public sealed class ProducesPortableAspNetCoreValidationProblemAttribute :
    ProducesResponseTypeAttribute<PortableAspNetCoreValidationProblemDetails>
{
    /// <summary>
    /// Initializes a new instance of
    /// <see cref="ProducesPortableAspNetCoreValidationProblemAttribute" />.
    /// </summary>
    /// <param name="statusCode">The HTTP status code (default 400 Bad Request).</param>
    /// <param name="contentType">The content type (default "application/problem+json").</param>
    public ProducesPortableAspNetCoreValidationProblemAttribute(
        int statusCode = StatusCodes.Status400BadRequest,
        string contentType = "application/problem+json"
    ) : base(statusCode, contentType) { }
}

/// <summary>
/// Specifies that the action produces an ASP.NET Core-compatible Light.PortableResults validation
/// problem details response with strongly typed metadata. Use this attribute when
/// <c>ValidationProblemSerializationFormat</c> is set to <c>AspNetCoreCompatible</c>. The response
/// type is documented as
/// <see cref="PortableAspNetCoreValidationProblemDetails{TErrorDetailMetadata, TProblemMetadata}" />.
/// </summary>
/// <typeparam name="TErrorDetailMetadata">The type of the metadata on each error details entry.</typeparam>
/// <typeparam name="TProblemMetadata">The type of the top-level problem metadata.</typeparam>
public sealed class ProducesPortableAspNetCoreValidationProblemAttribute<
    TErrorDetailMetadata,
    TProblemMetadata
> : ProducesResponseTypeAttribute<
    PortableAspNetCoreValidationProblemDetails<TErrorDetailMetadata, TProblemMetadata>
>
{
    /// <summary>
    /// Initializes a new instance of
    /// <see cref="ProducesPortableAspNetCoreValidationProblemAttribute{TErrorDetailMetadata, TProblemMetadata}" />.
    /// </summary>
    /// <param name="statusCode">The HTTP status code (default 400 Bad Request).</param>
    /// <param name="contentType">The content type (default "application/problem+json").</param>
    public ProducesPortableAspNetCoreValidationProblemAttribute(
        int statusCode = StatusCodes.Status400BadRequest,
        string contentType = "application/problem+json"
    ) : base(statusCode, contentType) { }
}
