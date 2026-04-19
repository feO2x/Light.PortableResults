namespace Light.PortableResults.AspNetCore.Shared;

/// <summary>
/// Successful response body that wraps a primary <typeparamref name="TValue" /> together with
/// a bag of <typeparamref name="TMetadata" />, for endpoints that opt into returning metadata
/// alongside the value.
/// </summary>
/// <typeparam name="TValue">The shape of the main payload.</typeparam>
/// <typeparam name="TMetadata">The shape of the metadata accompanying the payload.</typeparam>
/// <remarks>
/// Use this schema only when the runtime is configured to emit the <c>{ value, metadata }</c>
/// envelope (see <c>MetadataSerializationMode.Always</c>). For plain payload responses, use the
/// standard ASP.NET Core OpenAPI helpers such as <c>Produces&lt;TValue&gt;</c> or
/// <c>ProducesResponseTypeAttribute&lt;TValue&gt;</c> instead. This is a schema-only type used
/// by Light.PortableResults for OpenAPI documentation; the wire format is produced directly by
/// the runtime HTTP writers.
/// </remarks>
public class PortableSuccessResponse<TValue, TMetadata>
{
    /// <summary>
    /// The primary payload returned by the operation.
    /// </summary>
    public TValue Value { get; init; } = default!;

    /// <summary>
    /// Additional structured information associated with the payload, for example paging
    /// details or aggregate counts.
    /// </summary>
    public TMetadata Metadata { get; init; } = default!;
}
