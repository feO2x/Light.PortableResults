namespace Light.PortableResults.Metadata;

#pragma warning disable CS8524
/// <summary>
/// Describes the JSON shape of a metadata kind.
/// </summary>
public enum MetadataJsonShape : byte
{
    /// <summary>The JSON <c>null</c> shape.</summary>
    Null,

    /// <summary>The JSON boolean shape.</summary>
    Boolean,

    /// <summary>The JSON number shape.</summary>
    Number,

    /// <summary>The JSON string shape.</summary>
    String,

    /// <summary>The JSON array shape.</summary>
    Array,

    /// <summary>The JSON object shape.</summary>
    Object
}
