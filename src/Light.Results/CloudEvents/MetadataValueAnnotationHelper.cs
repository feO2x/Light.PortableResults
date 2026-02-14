using Light.Results.Metadata;

namespace Light.Results.CloudEvents;

/// <summary>
/// Provides helpers for creating metadata values with adjusted annotations.
/// </summary>
public static class MetadataValueAnnotationHelper
{
    /// <summary>
    /// Creates a new <see cref="MetadataValue" /> that has the same payload as <paramref name="value" />
    /// but with the specified <paramref name="annotation" />.
    /// </summary>
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
                return MetadataValue.FromNull(annotation);
        }
    }

    /// <summary>
    /// Creates a new <see cref="MetadataObject" /> where all contained values are rewritten with
    /// the provided <paramref name="annotation" />.
    /// </summary>
    public static MetadataObject WithAnnotation(MetadataObject metadataObject, MetadataValueAnnotation annotation)
    {
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
