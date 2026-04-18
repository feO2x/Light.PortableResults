using Light.PortableResults.AspNetCore.Shared;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Light.PortableResults.AspNetCore.Mvc;

/// <summary>
/// Specifies the response type for a successful action whose body contains both a
/// <typeparamref name="TValue" /> and a <typeparamref name="TMetadata" /> metadata object. The response
/// type is documented as <see cref="PortableSuccessResponse{TValue,TMetadata}" /> for OpenAPI purposes.
/// </summary>
/// <typeparam name="TValue">The type of the success value.</typeparam>
/// <typeparam name="TMetadata">The type of the success metadata.</typeparam>
public sealed class ProducesPortableSuccessResponseAttribute<TValue, TMetadata> :
    ProducesResponseTypeAttribute<PortableSuccessResponse<TValue, TMetadata>>
{
    /// <summary>
    /// Initializes a new instance of <see cref="ProducesPortableSuccessResponseAttribute{TValue, TMetadata}" />.
    /// </summary>
    /// <param name="statusCode">The HTTP status code (default 200).</param>
    /// <param name="contentType">The content type (default "application/json").</param>
    public ProducesPortableSuccessResponseAttribute(
        int statusCode = StatusCodes.Status200OK,
        string contentType = "application/json"
    ) : base(statusCode, contentType) { }
}
