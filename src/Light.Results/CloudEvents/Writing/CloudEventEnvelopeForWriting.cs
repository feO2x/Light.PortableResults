using System;
using Light.Results.Metadata;

namespace Light.Results.CloudEvents.Writing;

/// <summary>
/// Represents a CloudEvent envelope ready for JSON serialization for a non-generic <see cref="Result" /> payload.
/// </summary>
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
)
{
    /// <summary>
    /// Gets the CloudEvents specification version used by this integration.
    /// </summary>
    public static string SpecVersion => CloudEventConstants.SpecVersion;
}

/// <summary>
/// Represents a CloudEvent envelope ready for JSON serialization for a generic <see cref="Result{T}" /> payload.
/// </summary>
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
)
{
    /// <summary>
    /// Gets the CloudEvents specification version used by this integration.
    /// </summary>
    public static string SpecVersion => CloudEventConstants.SpecVersion;
}
