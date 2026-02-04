namespace Light.Results.Metadata;

/// <summary>
/// Discriminates the kind of value stored in a <see cref="MetadataValue" />.
/// </summary>
public enum MetadataKind : byte
{
    /// <summary>
    /// The metadata value represents a null reference/pointer. This is considered a primitive value.
    /// </summary>
    Null = 0,

    /// <summary>
    /// The metadata value represents a boolean value. This is considered a primitive value.
    /// </summary>
    Boolean = 1,

    /// <summary>
    /// The metadata value represents an integer number with 64 bits. This is considered a primitive value.
    /// </summary>
    Int64 = 2,

    /// <summary>
    /// The metadata value represents a floating-point number with 64 bits. This is considered a primitive value.
    /// </summary>
    Double = 3,

    /// <summary>
    /// The metadata value represents a string. This is considered a primitive value.
    /// </summary>
    String = 4,

    /// <summary>
    /// The metadata value represents an array, consisting of other metadata values. This is considered a complex value.
    /// </summary>
    Array = 5,

    /// <summary>
    /// The metadata value represents an object (a key-value store), consisting of other metadata values.
    /// This is considered a complex value.
    /// </summary>
    Object = 6
}

/// <summary>
/// Provides extension methods
/// </summary>
public static class MetadataKindExtensions
{
    /// <summary>
    /// Gets the value indicating whether the specified kind represents a primitive value.
    /// </summary>
    /// <param name="kind">The metadata kind.</param>
    /// <returns><see langword="true" /> if the kind is primitive; otherwise, <see langword="false" />.</returns>
    public static bool IsPrimitive(this MetadataKind kind) => kind < MetadataKind.Array;
}
