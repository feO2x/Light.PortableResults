using System;
using System.Collections.Generic;

namespace Light.PortableResults.Validation.Definitions;

/// <summary>
/// Represents an immutable lower and upper boundary pair for reusable validation rules.
/// </summary>
/// <typeparam name="T">The boundary type.</typeparam>
public readonly struct ValidationRange<T> : IEquatable<ValidationRange<T>>
{
    /// <summary>
    /// Initializes a new instance of <see cref="ValidationRange{T}" />.
    /// </summary>
    /// <param name="lowerBoundary">The inclusive lower boundary.</param>
    /// <param name="upperBoundary">The inclusive upper boundary.</param>
    public ValidationRange(T lowerBoundary, T upperBoundary)
    {
        LowerBoundary = lowerBoundary;
        UpperBoundary = upperBoundary;
    }

    /// <summary>
    /// Gets the inclusive lower boundary.
    /// </summary>
    public T LowerBoundary { get; }

    /// <summary>
    /// Gets the inclusive upper boundary.
    /// </summary>
    public T UpperBoundary { get; }

    /// <inheritdoc />
    public bool Equals(ValidationRange<T> other) =>
        EqualityComparer<T>.Default.Equals(LowerBoundary, other.LowerBoundary) &&
        EqualityComparer<T>.Default.Equals(UpperBoundary, other.UpperBoundary);

    /// <inheritdoc />
    public override bool Equals(object? obj) => obj is ValidationRange<T> other && Equals(other);

    /// <inheritdoc />
    public override int GetHashCode()
    {
        var hashCode = new HashCode();
        hashCode.Add(LowerBoundary, EqualityComparer<T>.Default);
        hashCode.Add(UpperBoundary, EqualityComparer<T>.Default);
        return hashCode.ToHashCode();
    }

    /// <summary>
    /// Determines whether two range values are equal.
    /// </summary>
    public static bool operator ==(ValidationRange<T> left, ValidationRange<T> right) => left.Equals(right);

    /// <summary>
    /// Determines whether two range values are not equal.
    /// </summary>
    public static bool operator !=(ValidationRange<T> left, ValidationRange<T> right) => !left.Equals(right);
}
