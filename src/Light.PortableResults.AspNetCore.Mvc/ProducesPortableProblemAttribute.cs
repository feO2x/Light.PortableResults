using Light.PortableResults.AspNetCore.Shared;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Light.PortableResults.AspNetCore.Mvc;

/// <summary>
/// Specifies that the action produces a Light.PortableResults problem details failure response with
/// untyped metadata. The response type is documented as <see cref="PortableProblemDetails" />.
/// </summary>
public sealed class ProducesPortableProblemAttribute : ProducesResponseTypeAttribute<PortableProblemDetails>
{
    /// <summary>
    /// Initializes a new instance of <see cref="ProducesPortableProblemAttribute" />.
    /// </summary>
    /// <param name="statusCode">The HTTP status code (default 500 Internal Server Error).</param>
    /// <param name="contentType">The content type (default "application/problem+json").</param>
    public ProducesPortableProblemAttribute(
        int statusCode = StatusCodes.Status500InternalServerError,
        string contentType = "application/problem+json"
    ) : base(statusCode, contentType) { }
}

/// <summary>
/// Specifies that the action produces a Light.PortableResults problem details failure response with
/// strongly typed metadata. The response type is documented as
/// <see cref="PortableProblemDetails{TErrorMetadata, TProblemMetadata}" />.
/// </summary>
/// <typeparam name="TErrorMetadata">The type of the metadata on each error.</typeparam>
/// <typeparam name="TProblemMetadata">The type of the top-level problem metadata.</typeparam>
public sealed class ProducesPortableProblemAttribute<TErrorMetadata, TProblemMetadata> :
    ProducesResponseTypeAttribute<PortableProblemDetails<TErrorMetadata, TProblemMetadata>>
{
    /// <summary>
    /// Initializes a new instance of
    /// <see cref="ProducesPortableProblemAttribute{TErrorMetadata, TProblemMetadata}" />.
    /// </summary>
    /// <param name="statusCode">The HTTP status code (default 500 Internal Server Error).</param>
    /// <param name="contentType">The content type (default "application/problem+json").</param>
    public ProducesPortableProblemAttribute(
        int statusCode = StatusCodes.Status500InternalServerError,
        string contentType = "application/problem+json"
    ) : base(statusCode, contentType) { }
}
