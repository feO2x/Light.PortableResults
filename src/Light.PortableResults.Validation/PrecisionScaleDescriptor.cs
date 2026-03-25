using System;

namespace Light.PortableResults.Validation;

/// <summary>
/// Describes the expected precision and scale of a decimal validation rule.
/// </summary>
public readonly struct PrecisionScaleDescriptor : IEquatable<PrecisionScaleDescriptor>
{
    /// <summary>
    /// Initializes a new instance of <see cref="PrecisionScaleDescriptor" />.
    /// </summary>
    /// <param name="precision">The expected total number of digits.</param>
    /// <param name="scale">The expected number of decimal digits.</param>
    /// <param name="ignoreTrailingZeros">
    /// Indicates whether trailing decimal zeros should be ignored when determining the actual scale.
    /// </param>
    public PrecisionScaleDescriptor(int precision, int scale, bool ignoreTrailingZeros)
    {
        Precision = precision;
        Scale = scale;
        IgnoreTrailingZeros = ignoreTrailingZeros;
    }

    /// <summary>
    /// Gets the expected total number of digits.
    /// </summary>
    public int Precision { get; }

    /// <summary>
    /// Gets the expected number of decimal digits.
    /// </summary>
    public int Scale { get; }

    /// <summary>
    /// Gets a value indicating whether trailing decimal zeros are ignored.
    /// </summary>
    public bool IgnoreTrailingZeros { get; }

    /// <inheritdoc />
    public bool Equals(PrecisionScaleDescriptor other) =>
        Precision == other.Precision &&
        Scale == other.Scale &&
        IgnoreTrailingZeros == other.IgnoreTrailingZeros;

    /// <inheritdoc />
    public override bool Equals(object? obj) =>
        obj is PrecisionScaleDescriptor other && Equals(other);

    /// <inheritdoc />
    public override int GetHashCode() => HashCode.Combine(Precision, Scale, IgnoreTrailingZeros);

    /// <summary>
    /// Compares two descriptors for equality.
    /// </summary>
    public static bool operator ==(PrecisionScaleDescriptor left, PrecisionScaleDescriptor right) =>
        left.Equals(right);

    /// <summary>
    /// Compares two descriptors for inequality.
    /// </summary>
    public static bool operator !=(PrecisionScaleDescriptor left, PrecisionScaleDescriptor right) =>
        !left.Equals(right);
}
