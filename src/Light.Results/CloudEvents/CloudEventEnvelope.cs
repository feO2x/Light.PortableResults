using System;
using Light.Results.Metadata;

namespace Light.Results.CloudEvents;

/// <summary>
/// Represents a parsed CloudEvent envelope containing a non-generic <see cref="Result" /> payload.
/// </summary>
public readonly record struct CloudEventEnvelope(
    string Type,
    string Source,
    string Id,
    Result Data,
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
/// Represents a parsed CloudEvent envelope containing a generic <see cref="Result{T}" /> payload.
/// </summary>
public readonly record struct CloudEventEnvelope<T>(
    string Type,
    string Source,
    string Id,
    Result<T> Data,
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
