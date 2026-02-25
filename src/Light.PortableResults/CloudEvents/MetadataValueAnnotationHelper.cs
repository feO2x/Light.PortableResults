using System;
using Light.PortableResults.Metadata;

namespace Light.PortableResults.CloudEvents;

/// <summary>
/// Provides helpers for creating metadata values with adjusted annotations.
/// </summary>
public static class MetadataValueAnnotationHelper
{
    /// <summary>
    /// Creates a new <see cref="MetadataValue" /> that has the same payload as <paramref name="value" />
    /// but with the specified <paramref name="annotation" />.
    /// </summary>
    /// <param name="value">The metadata value whose payload should be preserved.</param>
    /// <param name="annotation">The annotation to apply to the rewritten value and nested values.</param>
    /// <returns>
    /// A new <see cref="MetadataValue" /> containing the same payload as <paramref name="value" />
    /// with <paramref name="annotation" /> applied.
    /// </returns>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <paramref name="value" /> has an unsupported <see cref="MetadataKind" />.
    /// </exception>
    public static MetadataValue WithAnnotation(MetadataValue value, MetadataValueAnnotation annotation)
    {
        switch (value.Kind)
        {
            case MetadataKind.Null:
                return MetadataValue.FromNull(annotation);
            case MetadataKind.Boolean:
                value.TryGetBoolean(out var boolValue);
                return MetadataValue.FromBoolean(boolValue, annotation);
            case MetadataKind.Int64:
                value.TryGetInt64(out var int64Value);
                return MetadataValue.FromInt64(int64Value, annotation);
            case MetadataKind.Double:
                value.TryGetDouble(out var doubleValue);
                return MetadataValue.FromDouble(doubleValue, annotation);
            case MetadataKind.String:
                value.TryGetString(out var stringValue);
                return MetadataValue.FromString(stringValue, annotation);
            case MetadataKind.Array:
                value.TryGetArray(out var arrayValue);
                return MetadataValue.FromArray(WithAnnotation(arrayValue, annotation), annotation);
            case MetadataKind.Object:
                value.TryGetObject(out var objectValue);
                return MetadataValue.FromObject(WithAnnotation(objectValue, annotation), annotation);
            default:
                throw new ArgumentOutOfRangeException(nameof(value), value.Kind, "Unsupported metadata kind.");
        }
    }

    /// <summary>
    /// Creates a new <see cref="MetadataObject" /> where all contained values are rewritten with
    /// the provided <paramref name="annotation" />.
    /// </summary>
    /// <param name="metadataObject">The source metadata object.</param>
    /// <param name="annotation">The annotation to apply to all contained values recursively.</param>
    /// <returns>
    /// A rewritten <see cref="MetadataObject" /> with all contained values using
    /// <paramref name="annotation" />.
    /// </returns>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when one of the contained values has an unsupported <see cref="MetadataKind" />.
    /// </exception>
    public static MetadataObject WithAnnotation(MetadataObject metadataObject, MetadataValueAnnotation annotation)
    {
        if (metadataObject.Count == 0)
        {
            return metadataObject;
        }

        using var builder = MetadataObjectBuilder.Create(metadataObject.Count);
        foreach (var keyValuePair in metadataObject)
        {
            builder.Add(keyValuePair.Key, WithAnnotation(keyValuePair.Value, annotation));
        }

        return builder.Build();
    }

    private static MetadataArray WithAnnotation(MetadataArray array, MetadataValueAnnotation annotation)
    {
        using var builder = MetadataArrayBuilder.Create(array.Count);
        for (var i = 0; i < array.Count; i++)
        {
            builder.Add(WithAnnotation(array[i], annotation));
        }

        return builder.Build();
    }
}
