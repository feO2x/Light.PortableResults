using System;
using Light.PortableResults.Metadata;

namespace Light.PortableResults.CloudEvents;

/// <summary>
/// Identifies the JSON Event Format encoding used for a CloudEvents extension attribute value.
/// </summary>
/// <remarks>
/// <para>
/// These values describe JSON encodings, not the seven abstract CloudEvents context-attribute types.
/// <see cref="Null" /> represents an unset attribute and is omitted from the JSON object.
/// </para>
/// <para>
/// An <see cref="MetadataKind.Int64" /> uses <see cref="Integer" /> only while its value is in the
/// inclusive 32-bit signed range; values outside that range use <see cref="String" />. Publishers that
/// require a stable string shape for one attribute name can use a
/// <see cref="Writing.CloudEventsAttributeConverter" /> to convert its value to
/// <see cref="MetadataKind.String" /> before writing.
/// </para>
/// </remarks>
public enum CloudEventsAttributeJsonEncoding
{
    /// <summary>The attribute is unset and is omitted.</summary>
    Null,

    /// <summary>The value is written as a JSON Boolean.</summary>
    Boolean,

    /// <summary>The value is written as a JSON integer number.</summary>
    Integer,

    /// <summary>The value is written as a JSON string containing its canonical invariant text.</summary>
    String
}

#pragma warning disable CS8524 // Unnamed enum values intentionally throw SwitchExpressionException.

/// <summary>
/// Provides CloudEvents JSON Event Format encoding classification for metadata values.
/// </summary>
public static class CloudEventsAttributeJsonEncodingExtensions
{
    /// <summary>
    /// Gets the JSON Event Format encoding for a CloudEvents extension attribute value.
    /// </summary>
    /// <param name="value">The metadata value to classify.</param>
    /// <returns>The CloudEvents attribute JSON encoding.</returns>
    /// <remarks>
    /// <para>
    /// Boolean values use JSON booleans, signed integers in the inclusive 32-bit range use JSON numbers,
    /// and all other conforming non-null primitive values use JSON strings containing their canonical
    /// invariant text. Null represents an unset attribute and is omitted.
    /// </para>
    /// <para>
    /// The mapping is intentionally different from each metadata kind's natural JSON shape. On read-back,
    /// every string-mapped value is therefore initially represented as <see cref="MetadataKind.String" />.
    /// Register a <see cref="Reading.CloudEventsAttributeParser" /> when an extension attribute must be
    /// restored to a specific metadata kind.
    /// </para>
    /// </remarks>
    /// <exception cref="InvalidOperationException">
    /// Thrown when <paramref name="value" /> contains an array or object.
    /// </exception>
    public static CloudEventsAttributeJsonEncoding GetCloudEventsAttributeJsonEncoding(this MetadataValue value) =>
        value.Kind switch
        {
            MetadataKind.Null => CloudEventsAttributeJsonEncoding.Null,
            MetadataKind.Boolean => CloudEventsAttributeJsonEncoding.Boolean,
            MetadataKind.Int64 => GetInt64Encoding(value),
            MetadataKind.Double => CloudEventsAttributeJsonEncoding.String,
            MetadataKind.String => CloudEventsAttributeJsonEncoding.String,
            MetadataKind.Decimal => CloudEventsAttributeJsonEncoding.String,
            MetadataKind.UInt64 => CloudEventsAttributeJsonEncoding.String,
            MetadataKind.Single => CloudEventsAttributeJsonEncoding.String,
            MetadataKind.Char => CloudEventsAttributeJsonEncoding.String,
            MetadataKind.DateTime => CloudEventsAttributeJsonEncoding.String,
            MetadataKind.DateTimeOffset => CloudEventsAttributeJsonEncoding.String,
            MetadataKind.DateOnly => CloudEventsAttributeJsonEncoding.String,
            MetadataKind.TimeOnly => CloudEventsAttributeJsonEncoding.String,
            MetadataKind.TimeSpan => CloudEventsAttributeJsonEncoding.String,
            MetadataKind.Guid => CloudEventsAttributeJsonEncoding.String,
            MetadataKind.Uri => CloudEventsAttributeJsonEncoding.String,
            MetadataKind.Array => ThrowComplexValue(MetadataKind.Array),
            MetadataKind.Object => ThrowComplexValue(MetadataKind.Object)
        };

    private static CloudEventsAttributeJsonEncoding GetInt64Encoding(MetadataValue value)
    {
        value.TryGetInt64(out var int64Value);
        return int64Value is >= int.MinValue and <= int.MaxValue ?
            CloudEventsAttributeJsonEncoding.Integer :
            CloudEventsAttributeJsonEncoding.String;
    }

    private static CloudEventsAttributeJsonEncoding ThrowComplexValue(MetadataKind kind) =>
        throw new InvalidOperationException(
            $"CloudEvents extension attributes cannot encode metadata kind '{kind}'."
        );
}
