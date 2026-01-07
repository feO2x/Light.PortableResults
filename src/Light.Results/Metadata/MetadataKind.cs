namespace Light.Results.Metadata;

/// <summary>
/// Discriminates the kind of value stored in a <see cref="MetadataValue" />.
/// </summary>
public enum MetadataKind : byte
{
    Null = 0,
    Boolean = 1,
    Int64 = 2,
    Double = 3,
    String = 4,
    Array = 5,
    Object = 6
}
