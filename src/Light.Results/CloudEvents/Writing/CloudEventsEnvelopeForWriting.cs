using System;
using Light.Results.Metadata;

namespace Light.Results.CloudEvents.Writing;

/// <summary>
/// Represents a CloudEvents envelope ready for JSON serialization for a non-generic <see cref="Result" /> payload.
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
/// <param name="DataSchema">An optional URI reference to the schema that the data adheres to.</param>
/// <param name="ExtensionAttributes">Optional extension attributes to include with the cloud event.</param>
public readonly record struct CloudEventsEnvelopeForWriting(
    string Type,
    string Source,
    string Id,
    Result Data,
    ResolvedCloudEventsWriteOptions ResolvedOptions,
    string? Subject = null,
    DateTimeOffset? Time = null,
    string? DataSchema = null,
    MetadataObject? ExtensionAttributes = null
) : ICloudEventsEnvelopeForWritingWithMetadata<Result>
{
    /// <summary>
    /// Gets the CloudEvents specification version used by this integration.
    /// </summary>
    public static string SpecVersion => CloudEventsConstants.SpecVersion;
}

/// <summary>
/// Represents a CloudEvents envelope ready for JSON serialization for a generic <see cref="Result{T}" /> payload.
/// </summary>
/// <param name="Type">The CloudEvents type that describes the semantic meaning of the event.</param>
/// <param name="Source">A URI-like identifier that describes the origin of the event.</param>
/// <param name="Id">A unique identifier for the event instance.</param>
/// <param name="Data">The typed <see cref="Result{T}" /> payload to serialize as the CloudEvents data.</param>
/// <param name="ResolvedOptions">Resolved CloudEvents write options controlling serialization details.</param>
/// <param name="Subject">
/// An optional subject that further qualifies the event. We highly recommend to set this value for routing and
/// observability purposes.
/// </param>
/// <param name="Time">An optional timestamp representing when the event occurred.</param>
/// <param name="DataSchema">An optional URI reference to the schema that the data adheres to.</param>
/// <param name="ExtensionAttributes">Optional extension attributes to include with the cloud event.</param>
public readonly record struct CloudEventsEnvelopeForWriting<T>(
    string Type,
    string Source,
    string Id,
    Result<T> Data,
    ResolvedCloudEventsWriteOptions ResolvedOptions,
    string? Subject = null,
    DateTimeOffset? Time = null,
    string? DataSchema = null,
    MetadataObject? ExtensionAttributes = null
) : ICloudEventsEnvelopeForWritingWithMetadata<Result<T>>
{
    /// <summary>
    /// Gets the CloudEvents specification version used by this integration.
    /// </summary>
    public static string SpecVersion => CloudEventsConstants.SpecVersion;
}
