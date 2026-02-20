using System.Text.Json;
using Light.Results.Metadata;

namespace Light.Results.Http.Reading.Json;

/// <summary>
/// Provides low-level JSON parsing helpers for metadata values.
/// </summary>
public static class MetadataJsonReader
{
    /// <summary>
    /// Reads a <see cref="MetadataValue" /> from the current JSON token.
    /// </summary>
    /// <param name="reader">The JSON reader.</param>
    /// <param name="annotation">The annotation applied to parsed values.</param>
    /// <returns>The parsed metadata value.</returns>
    public static MetadataValue ReadMetadataValue(
        ref Utf8JsonReader reader,
        MetadataValueAnnotation annotation = MetadataValueAnnotation.SerializeInHttpResponseBody
    ) =>
        SharedJsonSerialization.Reading.MetadataJsonReader.ReadMetadataValue(ref reader, annotation);

    /// <summary>
    /// Reads a <see cref="MetadataObject" /> from the current JSON token.
    /// </summary>
    /// <param name="reader">The JSON reader.</param>
    /// <param name="annotation">The annotation applied to parsed values.</param>
    /// <returns>The parsed metadata object.</returns>
    public static MetadataObject ReadMetadataObject(
        ref Utf8JsonReader reader,
        MetadataValueAnnotation annotation = MetadataValueAnnotation.SerializeInHttpResponseBody
    ) =>
        SharedJsonSerialization.Reading.MetadataJsonReader.ReadMetadataObject(ref reader, annotation);

    /// <summary>
    /// Reads a <see cref="MetadataArray" /> from the current JSON token.
    /// </summary>
    /// <param name="reader">The JSON reader.</param>
    /// <param name="annotation">The annotation applied to parsed values.</param>
    /// <returns>The parsed metadata array.</returns>
    public static MetadataArray ReadMetadataArray(
        ref Utf8JsonReader reader,
        MetadataValueAnnotation annotation = MetadataValueAnnotation.SerializeInHttpResponseBody
    ) =>
        SharedJsonSerialization.Reading.MetadataJsonReader.ReadMetadataArray(ref reader, annotation);
}
