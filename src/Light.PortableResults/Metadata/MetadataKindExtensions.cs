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

    /// <summary>
    /// Gets the .NET numeric type that the specified metadata kind is written from, or
    /// <see cref="MetadataNumberEncoding.None" /> when the kind does not have the
    /// <see cref="MetadataJsonShape.Number" /> shape.
    /// </summary>
    /// <param name="kind">The metadata kind.</param>
    /// <returns>The number encoding.</returns>
    /// <remarks>
    /// This is an exhaustive switch on purpose. A kind added to <see cref="GetJsonShape" /> with the
    /// <see cref="MetadataJsonShape.Number" /> shape but forgotten here would otherwise reach the JSON writer
    /// as an unwritable number, so both classifications fail the Release build together.
    /// </remarks>
    public static MetadataNumberEncoding GetNumberEncoding(this MetadataKind kind) =>
        kind switch
        {
            MetadataKind.Null => MetadataNumberEncoding.None,
            MetadataKind.Boolean => MetadataNumberEncoding.None,
            MetadataKind.Int64 => MetadataNumberEncoding.Int64,
            MetadataKind.Double => MetadataNumberEncoding.Double,
            MetadataKind.String => MetadataNumberEncoding.None,
            MetadataKind.Decimal => MetadataNumberEncoding.Decimal,
            MetadataKind.UInt64 => MetadataNumberEncoding.None,
            MetadataKind.Single => MetadataNumberEncoding.Single,
            MetadataKind.Char => MetadataNumberEncoding.None,
            MetadataKind.DateTime => MetadataNumberEncoding.None,
            MetadataKind.DateTimeOffset => MetadataNumberEncoding.None,
            MetadataKind.DateOnly => MetadataNumberEncoding.None,
            MetadataKind.TimeOnly => MetadataNumberEncoding.None,
            MetadataKind.TimeSpan => MetadataNumberEncoding.None,
            MetadataKind.Guid => MetadataNumberEncoding.None,
            MetadataKind.Uri => MetadataNumberEncoding.None,
            MetadataKind.Array => MetadataNumberEncoding.None,
            MetadataKind.Object => MetadataNumberEncoding.None
        };
}
