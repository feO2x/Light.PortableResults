using System;
using Light.Results.Metadata;

namespace Light.Results.CloudEvents;

/// <summary>
/// Represents a parsed CloudEvents envelope containing a non-generic <see cref="Result" /> payload.
/// </summary>
/// <param name="Type">The CloudEvents type attribute identifying the type of event.</param>
/// <param name="Source">The CloudEvents source attribute identifying the context in which the event happened.</param>
/// <param name="Id">The CloudEvents id attribute uniquely identifying this event.</param>
/// <param name="Data">The result payload of the CloudEvents envelope.</param>
/// <param name="Subject">The optional CloudEvents subject attribute.</param>
/// <param name="Time">The optional CloudEvents time attribute.</param>
/// <param name="DataContentType">The optional CloudEvents datacontenttype attribute.</param>
/// <param name="DataSchema">The optional CloudEvents dataschema attribute.</param>
/// <param name="ExtensionAttributes">The optional CloudEvents extension attributes.</param>
public readonly record struct CloudEventsEnvelope(
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
    public static string SpecVersion => CloudEventsConstants.SpecVersion;
}

/// <summary>
/// Represents a parsed CloudEvents envelope containing a generic <see cref="Result{T}" /> payload.
/// </summary>
/// <typeparam name="T">The type of the success value in the result.</typeparam>
/// <param name="Type">The CloudEvents type attribute identifying the type of event.</param>
/// <param name="Source">The CloudEvents source attribute identifying the context in which the event happened.</param>
/// <param name="Id">The CloudEvents id attribute uniquely identifying this event.</param>
/// <param name="Data">The result payload of the CloudEvents envelope.</param>
/// <param name="Subject">The optional CloudEvents subject attribute.</param>
/// <param name="Time">The optional CloudEvents time attribute.</param>
/// <param name="DataContentType">The optional CloudEvents datacontenttype attribute.</param>
/// <param name="DataSchema">The optional CloudEvents dataschema attribute.</param>
/// <param name="ExtensionAttributes">The optional CloudEvents extension attributes.</param>
public readonly record struct CloudEventsEnvelope<T>(
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
    public static string SpecVersion => CloudEventsConstants.SpecVersion;
}
