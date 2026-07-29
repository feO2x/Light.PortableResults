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
    /// <exception cref="System.ArgumentException">
    /// Thrown when <paramref name="annotation" /> cannot be applied to an array or object value.
    /// </exception>
    /// <exception cref="System.InvalidOperationException">
    /// Thrown as a <c>SwitchExpressionException</c> when <paramref name="value" /> has an undeclared
    /// <see cref="MetadataKind" />.
    /// </exception>
    public static MetadataValue WithAnnotation(MetadataValue value, MetadataValueAnnotation annotation)
    {
        // JsonShape throws SwitchExpressionException for an undeclared kind, so the default arm only ever
        // sees the four primitive shapes.
        switch (value.JsonShape)
        {
            case MetadataJsonShape.Array:
                value.TryGetArray(out var arrayValue);
                return MetadataValue.FromArray(WithAnnotation(arrayValue, annotation), annotation);
            case MetadataJsonShape.Object:
                value.TryGetObject(out var objectValue);
                return MetadataValue.FromObject(WithAnnotation(objectValue, annotation), annotation);
            default:
                return value.WithAnnotation(annotation);
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
    /// <exception cref="System.ArgumentException">
    /// Thrown when <paramref name="annotation" /> cannot be applied to a contained array or object value.
    /// </exception>
    /// <exception cref="System.InvalidOperationException">
    /// Thrown as a <c>SwitchExpressionException</c> when one of the contained values has an undeclared
    /// <see cref="MetadataKind" />.
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
