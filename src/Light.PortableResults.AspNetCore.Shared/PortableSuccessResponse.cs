namespace Light.PortableResults.AspNetCore.Shared;

/// <summary>
/// Schema-only type for OpenAPI documentation. Not used at runtime.
/// Represents a successful response body that contains both a value and metadata.
/// Use this helper only for wrapped success responses that serialize metadata in the body.
/// For plain <typeparamref name="TValue" /> success bodies, use the standard ASP.NET Core
/// OpenAPI metadata APIs such as <c>Produces&lt;TValue&gt;</c> or
/// <c>ProducesResponseTypeAttribute&lt;TValue&gt;</c> instead.
/// </summary>
/// <typeparam name="TValue">The type of the success value.</typeparam>
/// <typeparam name="TMetadata">The type of the success metadata.</typeparam>
public class PortableSuccessResponse<TValue, TMetadata>
{
    /// <summary>
    /// Gets or sets the result value.
    /// </summary>
    public TValue Value { get; init; } = default!;

    /// <summary>
    /// Gets or sets the metadata associated with the value.
    /// </summary>
    public TMetadata Metadata { get; init; } = default!;
}
