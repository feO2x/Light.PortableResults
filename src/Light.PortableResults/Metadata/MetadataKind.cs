namespace Light.PortableResults.Metadata;

/// <summary>
/// <para>
/// Discriminates the kind of value stored in a <see cref="MetadataValue" />.
/// </para>
/// <para>
/// The numeric values of the members are a hard constraint:
/// <see cref="MetadataKindExtensions.IsPrimitive" /> decides membership of the primitive set with a single
/// comparison against <see cref="Array" />. All primitive kinds must therefore be declared with values below
/// <see cref="Array" />. The values 6 to 199 are reserved for future primitive kinds (CloudEvents, for example,
/// defines Binary, URI, URI-reference, and Timestamp attribute types that are currently flattened into
/// <see cref="String" />), so that adding one of them later does not renumber the complex kinds again.
/// </para>
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
    /// The metadata value represents a decimal number with 128 bits. This is considered a primitive value.
    /// </summary>
    Decimal = 5,

    // 6 - 199 are reserved for future primitive kinds. Do not declare a primitive kind at 200 or above,
    // and do not declare a complex kind below 200 - see the remarks on this enum for details.

    /// <summary>
    /// The metadata value represents an array, consisting of other metadata values. This is considered a complex value.
    /// </summary>
    Array = 200,

    /// <summary>
    /// The metadata value represents an object (a key-value store), consisting of other metadata values.
    /// This is considered a complex value.
    /// </summary>
    Object = 201
}

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
}
