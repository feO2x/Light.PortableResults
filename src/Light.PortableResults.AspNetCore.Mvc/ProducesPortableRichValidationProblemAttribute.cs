using Light.PortableResults.AspNetCore.Shared;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Light.PortableResults.AspNetCore.Mvc;

/// <summary>
/// Specifies that the action produces a rich Light.PortableResults validation problem details response
/// with untyped metadata. Use this attribute when <c>ValidationProblemSerializationFormat</c> is set
/// to <c>Rich</c>. The response type is documented as
/// <see cref="PortableRichValidationProblemDetails" />.
/// </summary>
public sealed class ProducesPortableRichValidationProblemAttribute :
    ProducesResponseTypeAttribute<PortableRichValidationProblemDetails>
{
    /// <summary>
    /// Initializes a new instance of <see cref="ProducesPortableRichValidationProblemAttribute" />.
    /// </summary>
    /// <param name="statusCode">The HTTP status code (default 400 Bad Request).</param>
    /// <param name="contentType">The content type (default "application/problem+json").</param>
    public ProducesPortableRichValidationProblemAttribute(
        int statusCode = StatusCodes.Status400BadRequest,
        string contentType = "application/problem+json"
    ) : base(statusCode, contentType) { }
}

/// <summary>
/// Specifies that the action produces a rich Light.PortableResults validation problem details response
/// with strongly typed metadata. Use this attribute when <c>ValidationProblemSerializationFormat</c>
/// is set to <c>Rich</c>. The response type is documented as
/// <see cref="PortableRichValidationProblemDetails{TErrorMetadata, TProblemMetadata}" />.
/// </summary>
/// <typeparam name="TErrorMetadata">The type of the metadata on each validation error.</typeparam>
/// <typeparam name="TProblemMetadata">The type of the top-level problem metadata.</typeparam>
public sealed class ProducesPortableRichValidationProblemAttribute<TErrorMetadata, TProblemMetadata> :
    ProducesResponseTypeAttribute<PortableRichValidationProblemDetails<TErrorMetadata, TProblemMetadata>>
{
    /// <summary>
    /// Initializes a new instance of
    /// <see cref="ProducesPortableRichValidationProblemAttribute{TErrorMetadata, TProblemMetadata}" />.
    /// </summary>
    /// <param name="statusCode">The HTTP status code (default 400 Bad Request).</param>
    /// <param name="contentType">The content type (default "application/problem+json").</param>
    public ProducesPortableRichValidationProblemAttribute(
        int statusCode = StatusCodes.Status400BadRequest,
        string contentType = "application/problem+json"
    ) : base(statusCode, contentType) { }
}
