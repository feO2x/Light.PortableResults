using Light.Results.AspNetCore.Shared;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Light.Results.AspNetCore.Mvc;

/// <summary>
/// Specifies the type of the value and metadata returned by the action for OpenAPI documentation.
/// The response type is documented as <see cref="WrappedResponse{TValue,TMetadata}" />.
/// </summary>
/// <typeparam name="TValue">The type of the result value.</typeparam>
/// <typeparam name="TMetadata">The type of the metadata (for OpenAPI schema generation).</typeparam>
public sealed class ProducesLightResultAttribute<TValue, TMetadata>
    : ProducesResponseTypeAttribute<WrappedResponse<TValue, TMetadata>>
{
    /// <summary>
    /// Initializes a new instance of <see cref="ProducesLightResultAttribute{TValue, TMetadata}" />.
    /// </summary>
    /// <param name="statusCode">The HTTP status code (default 200).</param>
    /// <param name="contentType">The content type (default "application/json").</param>
    public ProducesLightResultAttribute(
        int statusCode = StatusCodes.Status200OK,
        string contentType = "application/json"
    ) : base(statusCode, contentType) { }
}

/// <summary>
/// Specifies the type of the value returned by the action for OpenAPI documentation.
/// The metadata will be documented as an object with additionalProperties.
/// The response type is documented as <see cref="WrappedResponse{TValue, TMetadata}" /> with <c>object</c> metadata.
/// </summary>
/// <typeparam name="TValue">The type of the result value.</typeparam>
public sealed class ProducesLightResultAttribute<TValue>
    : ProducesResponseTypeAttribute<WrappedResponse<TValue, object>>
{
    /// <summary>
    /// Initializes a new instance of <see cref="ProducesLightResultAttribute{TValue}" />.
    /// </summary>
    /// <param name="statusCode">The HTTP status code (default 200).</param>
    /// <param name="contentType">The content type (default "application/json").</param>
    public ProducesLightResultAttribute(
        int statusCode = StatusCodes.Status200OK,
        string contentType = "application/json"
    ) : base(statusCode, contentType) { }
}
