namespace Light.PortableResults.Metadata;

/// <summary>
/// <para>
/// Discriminates the kind of value stored in a <see cref="MetadataValue" />.
/// </para>
/// <para>
/// The numeric values of the members are a hard constraint:
/// <see cref="MetadataKindExtensions.IsPrimitive" /> decides membership of the primitive set with a single
/// comparison against <see cref="Array" />. All primitive kinds must therefore be declared with values below
/// <see cref="Array" />. Values 16 to 199 are reserved for future primitive kinds, so that adding one of them
/// later does not renumber the complex kinds again.
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

    /// <summary>
    /// The metadata value represents an unsigned integer number with 64 bits. This is considered a primitive value.
    /// </summary>
    UInt64 = 6,

    /// <summary>
    /// The metadata value represents a floating-point number with 32 bits. This is considered a primitive value.
    /// </summary>
    Single = 7,

    /// <summary>
    /// The metadata value represents a UTF-16 character. This is considered a primitive value.
    /// </summary>
    Char = 8,

    /// <summary>
    /// The metadata value represents a UTC date and time. This is considered a primitive value.
    /// </summary>
    DateTime = 9,

    /// <summary>
    /// The metadata value represents a date and time with an offset. This is considered a primitive value.
    /// </summary>
    DateTimeOffset = 10,

    /// <summary>
    /// The metadata value represents a date without a time. This is considered a primitive value.
    /// </summary>
    DateOnly = 11,

    /// <summary>
    /// The metadata value represents a time without a date. This is considered a primitive value.
    /// </summary>
    TimeOnly = 12,

    /// <summary>
    /// The metadata value represents a duration. This is considered a primitive value.
    /// </summary>
    TimeSpan = 13,

    /// <summary>
    /// The metadata value represents a globally unique identifier. This is considered a primitive value.
    /// </summary>
    Guid = 14,

    /// <summary>
    /// The metadata value represents a URI reference. This is considered a primitive value.
    /// </summary>
    Uri = 15,

    // 16 - 199 are reserved for future primitive kinds. Do not declare a primitive kind at 200 or above,
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
