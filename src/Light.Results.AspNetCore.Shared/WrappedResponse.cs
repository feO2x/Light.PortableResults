namespace Light.Results.AspNetCore.Shared;

/// <summary>
/// Schema-only type for OpenAPI documentation. Not used at runtime.
/// Represents a wrapped response with a value and metadata.
/// </summary>
/// <typeparam name="TValue">The type of the value.</typeparam>
/// <typeparam name="TMetadata">The type of the metadata.</typeparam>
public sealed class WrappedResponse<TValue, TMetadata>
{
    /// <summary>
    /// Gets or sets the result value.
    /// </summary>
    public TValue Value { get; init; } = default!;

    /// <summary>
    /// Gets or sets the metadata.
    /// </summary>
    public TMetadata Metadata { get; init; } = default!;
}
