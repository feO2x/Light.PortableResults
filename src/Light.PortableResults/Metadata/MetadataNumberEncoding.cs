namespace Light.PortableResults.Metadata;

/// <summary>
/// <para>
/// Describes which .NET numeric type a metadata kind is written from when its JSON shape is
/// <see cref="MetadataJsonShape.Number" />.
/// </para>
/// <para>
/// <see cref="MetadataJsonShape" /> tells a serializer that a value is a JSON number;
/// this tells it which primitive overload to write it with. Serializers for other protocols need the same
/// distinction, which is why it is public rather than an implementation detail of the JSON writer.
/// </para>
/// </summary>
public enum MetadataNumberEncoding : byte
{
    /// <summary>The kind does not have the <see cref="MetadataJsonShape.Number" /> shape.</summary>
    None,

    /// <summary>The value is written from a <see cref="long" />.</summary>
    Int64,

    /// <summary>The value is written from a <see cref="double" />.</summary>
    Double,

    /// <summary>The value is written from a <see cref="float" />.</summary>
    Single,

    /// <summary>The value is written from a <see cref="decimal" />.</summary>
    Decimal
}
