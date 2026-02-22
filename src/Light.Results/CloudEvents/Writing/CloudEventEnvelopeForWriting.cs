using System;
using Light.Results.Metadata;

namespace Light.Results.CloudEvents.Writing;

/// <summary>
/// Represents a CloudEvent envelope ready for JSON serialization for a non-generic <see cref="Result" /> payload.
/// </summary>
/// <param name="Type">The CloudEvents type that describes the semantic meaning of the event.</param>
/// <param name="Source">A URI-like identifier that describes the origin of the event.</param>
/// <param name="Id">A unique identifier for the event instance.</param>
/// <param name="Data">The <see cref="Result" /> payload to serialize as the CloudEvents data.</param>
/// <param name="ResolvedOptions">Resolved CloudEvents write options controlling serialization details.</param>
/// <param name="Subject">
/// An optional subject that further qualifies the event. We highly recommend to set this value for routing and
/// observability purposes.
/// </param>
/// <param name="Time">An optional timestamp representing when the event occurred.</param>
/// <param name="DataContentType">The optional content type that describes the data payload.</param>
/// <param name="DataSchema">An optional URI reference to the schema that the data adheres to.</param>
/// <param name="ExtensionAttributes">Optional extension attributes to include with the CloudEvent.</param>
public readonly record struct CloudEventEnvelopeForWriting(
    string Type,
    string Source,
    string Id,
    Result Data,
    ResolvedCloudEventWriteOptions ResolvedOptions,
    string? Subject = null,
    DateTimeOffset? Time = null,
    string? DataContentType = null,
    string? DataSchema = null,
    MetadataObject? ExtensionAttributes = null
) : ICloudEventEnvelopeForWritingWithMetadata<Result>
{
    /// <summary>
    /// Gets the CloudEvents specification version used by this integration.
    /// </summary>
    public static string SpecVersion => CloudEventConstants.SpecVersion;
}

/// <summary>
/// Represents a CloudEvent envelope ready for JSON serialization for a generic <see cref="Result{T}" /> payload.
/// </summary>
/// <param name="Type">The CloudEvent type that describes the semantic meaning of the event.</param>
/// <param name="Source">A URI-like identifier that describes the origin of the event.</param>
/// <param name="Id">A unique identifier for the event instance.</param>
/// <param name="Data">The typed <see cref="Result{T}" /> payload to serialize as the CloudEvent data.</param>
/// <param name="ResolvedOptions">Resolved CloudEvent write options controlling serialization details.</param>
/// <param name="Subject">
/// An optional subject that further qualifies the event. We highly recommend to set this value for routing and
/// observability purposes.
/// </param>
/// <param name="Time">An optional timestamp representing when the event occurred.</param>
/// <param name="DataContentType">The optional content type that describes the data payload.</param>
/// <param name="DataSchema">An optional URI reference to the schema that the data adheres to.</param>
/// <param name="ExtensionAttributes">Optional extension attributes to include with the CloudEvent.</param>
public readonly record struct CloudEventEnvelopeForWriting<T>(
    string Type,
    string Source,
    string Id,
    Result<T> Data,
    ResolvedCloudEventWriteOptions ResolvedOptions,
    string? Subject = null,
    DateTimeOffset? Time = null,
    string? DataContentType = null,
    string? DataSchema = null,
    MetadataObject? ExtensionAttributes = null
) : ICloudEventEnvelopeForWritingWithMetadata<Result<T>>
{
    /// <summary>
    /// Gets the CloudEvents specification version used by this integration.
    /// </summary>
    public static string SpecVersion => CloudEventConstants.SpecVersion;
}
