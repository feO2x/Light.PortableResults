namespace Light.PortableResults.Metadata;

#pragma warning disable CS8524
/// <summary>
/// Provides extension methods for <see cref="MetadataKind" />.
/// </summary>
public static class MetadataKindExtensions
{
    /// <summary>
    /// Gets the value indicating whether the specified kind represents a primitive value.
    /// </summary>
    /// <param name="kind">The metadata kind.</param>
    /// <returns><see langword="true" /> if the kind is primitive; otherwise, <see langword="false" />.</returns>
    public static bool IsPrimitive(this MetadataKind kind) => kind < MetadataKind.Array;

    /// <summary>
    /// Gets the JSON shape represented by the specified metadata kind.
    /// </summary>
    /// <param name="kind">The metadata kind.</param>
    /// <returns>The JSON shape.</returns>
    public static MetadataJsonShape GetJsonShape(this MetadataKind kind) =>
        kind switch
        {
            MetadataKind.Null => MetadataJsonShape.Null,
            MetadataKind.Boolean => MetadataJsonShape.Boolean,
            MetadataKind.Int64 => MetadataJsonShape.Number,
            MetadataKind.Double => MetadataJsonShape.Number,
            MetadataKind.String => MetadataJsonShape.String,
            MetadataKind.Decimal => MetadataJsonShape.Number,
            MetadataKind.UInt64 => MetadataJsonShape.String,
            MetadataKind.Single => MetadataJsonShape.Number,
            MetadataKind.Char => MetadataJsonShape.String,
            MetadataKind.DateTime => MetadataJsonShape.String,
            MetadataKind.DateTimeOffset => MetadataJsonShape.String,
            MetadataKind.DateOnly => MetadataJsonShape.String,
            MetadataKind.TimeOnly => MetadataJsonShape.String,
            MetadataKind.TimeSpan => MetadataJsonShape.String,
            MetadataKind.Guid => MetadataJsonShape.String,
            MetadataKind.Uri => MetadataJsonShape.String,
            MetadataKind.Array => MetadataJsonShape.Array,
            MetadataKind.Object => MetadataJsonShape.Object
        };
}
