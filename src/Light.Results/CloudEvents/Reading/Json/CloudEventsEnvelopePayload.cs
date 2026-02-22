using System;
using Light.Results.Metadata;

namespace Light.Results.CloudEvents.Reading.Json;

/// <summary>
/// Represents a parsed CloudEvents envelope with indicators for the Cloud Events <c>data</c> property.
/// </summary>
/// <param name="Type">The CloudEvents type attribute.</param>
/// <param name="Source">The CloudEvents source attribute.</param>
/// <param name="Id">The CloudEvents id attribute.</param>
/// <param name="Subject">The optional CloudEvents subject attribute.</param>
/// <param name="Time">The optional CloudEvents time attribute.</param>
/// <param name="DataContentType">The optional CloudEvents datacontenttype attribute.</param>
/// <param name="DataSchema">The optional CloudEvents dataschema attribute.</param>
/// <param name="ExtensionAttributes">The parsed extension attributes.</param>
/// <param name="HasData">A value indicating whether the data property was present in the envelope.</param>
/// <param name="IsDataNull">A value indicating whether the data property was null.</param>
/// <param name="DataStart">The byte offset where the data value begins in the original buffer.</param>
/// <param name="DataLength">The length of the data value in bytes.</param>
public readonly record struct CloudEventsEnvelopePayload(
    string Type,
    string Source,
    string Id,
    string? Subject,
    DateTimeOffset? Time,
    string? DataContentType,
    string? DataSchema,
    MetadataObject? ExtensionAttributes,
    bool HasData,
    bool IsDataNull,
    int DataStart,
    int DataLength
);
