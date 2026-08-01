using System;
using System.Diagnostics.CodeAnalysis;
using Light.PortableResults.Metadata;
using Microsoft.Extensions.Primitives;

namespace Light.PortableResults.Http.Writing.Headers;

/// <summary>
/// Formats metadata values for use as HTTP header values.
/// </summary>
/// <remarks>
/// A top-level null and an empty array produce no header values. A single-item array is indistinguishable
/// from a scalar after parsing, while an empty array cannot be reconstructed because no header is emitted.
/// Null array items are preserved as the canonical text <c>null</c>.
/// </remarks>
public static class HttpHeaderValueFormatter
{
    /// <summary>
    /// Formats the specified metadata value as zero, one, or multiple HTTP header values.
    /// </summary>
    /// <param name="value">The metadata value to format.</param>
    /// <returns>The formatted HTTP header values.</returns>
    /// <exception cref="NotSupportedException">
    /// Thrown when <paramref name="value" /> is an object or an array containing a complex child.
    /// </exception>
#pragma warning disable CS8524 // Unnamed enum values intentionally throw.
    public static StringValues Format(MetadataValue value) =>
        value.Kind switch
        {
            MetadataKind.Null => StringValues.Empty,
            MetadataKind.Boolean => value.ToCanonicalString(),
            MetadataKind.Int64 => value.ToCanonicalString(),
            MetadataKind.Double => value.ToCanonicalString(),
            MetadataKind.String => value.ToCanonicalString(),
            MetadataKind.Decimal => value.ToCanonicalString(),
            MetadataKind.UInt64 => value.ToCanonicalString(),
            MetadataKind.Single => value.ToCanonicalString(),
            MetadataKind.Char => value.ToCanonicalString(),
            MetadataKind.DateTime => value.ToCanonicalString(),
            MetadataKind.DateTimeOffset => value.ToCanonicalString(),
            MetadataKind.DateOnly => value.ToCanonicalString(),
            MetadataKind.TimeOnly => value.ToCanonicalString(),
            MetadataKind.TimeSpan => value.ToCanonicalString(),
            MetadataKind.Guid => value.ToCanonicalString(),
            MetadataKind.Uri => value.ToCanonicalString(),
            MetadataKind.Array => FormatArray(value.AsArray()),
            MetadataKind.Object => ThrowUnsupportedComplexValue(MetadataKind.Object)
        };
#pragma warning restore CS8524

    private static StringValues FormatArray(MetadataArray array)
    {
        var values = array.AsSpan();
        switch (values.Length)
        {
            case 0: return StringValues.Empty;
            case 1: return FormatArrayItem(values[0]);
        }

        var formattedValues = new string[values.Length];
        for (var i = 0; i < values.Length; i++)
        {
            formattedValues[i] = FormatArrayItem(values[i]);
        }

        return new StringValues(formattedValues);
    }

    private static string FormatArrayItem(MetadataValue value) =>
        value.Kind.IsPrimitive() ? value.ToCanonicalString() : ThrowUnsupportedComplexArrayItem(value.Kind);

    [DoesNotReturn]
    private static StringValues ThrowUnsupportedComplexValue(MetadataKind kind) =>
        throw new NotSupportedException($"Metadata values of kind {kind} cannot be formatted as HTTP header values.");

    [DoesNotReturn]
    private static string ThrowUnsupportedComplexArrayItem(MetadataKind kind) =>
        throw new NotSupportedException(
            $"Metadata arrays containing children of kind {kind} cannot be formatted as HTTP header values."
        );
}
