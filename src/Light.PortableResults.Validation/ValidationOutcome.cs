using System;
using System.Diagnostics.CodeAnalysis;

namespace Light.PortableResults.Validation;

/// <summary>
/// Represents the outcome of a validation run, preserving the validated value on success and flat errors on failure.
/// </summary>
/// <typeparam name="T">The type of the validated value.</typeparam>
public readonly struct ValidationOutcome<T> : IEquatable<ValidationOutcome<T>>
{
    /// <summary>
    /// Initializes a new successful validation outcome.
    /// </summary>
    /// <param name="value">The validated value.</param>
    public ValidationOutcome(T value)
        : this(value, default) { }

    /// <summary>
    /// Initializes a new validation outcome.
    /// </summary>
    /// <param name="value">The best available validated value.</param>
    /// <param name="errors">The validation errors.</param>
    public ValidationOutcome(T value, Errors errors)
    {
        if (!errors.IsEmpty || value is not null || typeof(T).IsValueType)
        {
            Value = value;
            Errors = errors;
            return;
        }

        Value = value;
        Errors = errors;
    }

    /// <summary>
    /// Gets the validated value or the best available value from the validation pipeline.
    /// </summary>
    [AllowNull]
    public T Value { get; }

    /// <summary>
    /// Gets the validation errors. The empty <see cref="Errors" /> value indicates success.
    /// </summary>
    public Errors Errors { get; }

    /// <summary>
    /// Gets a value indicating whether the validation outcome is valid.
    /// </summary>
    public bool IsValid => Errors.IsEmpty;

    /// <summary>
    /// Gets a value indicating whether the validation outcome contains errors.
    /// </summary>
    public bool HasErrors => !Errors.IsEmpty;

    /// <summary>
    /// Attempts to get the validated value on success.
    /// </summary>
    /// <param name="value">The validated value on success; otherwise the default value.</param>
    /// <returns><see langword="true" /> on success; otherwise, <see langword="false" />.</returns>
    public bool TryGetValue([MaybeNullWhen(false)] out T value)
    {
        value = Value;
        return IsValid;
    }

    /// <summary>
    /// Converts a failed validation outcome into a failure <see cref="Result" />.
    /// </summary>
    /// <returns>The failure result.</returns>
    /// <exception cref="InvalidOperationException">Thrown when called on a valid outcome.</exception>
    public Result ToFailureResult()
    {
        if (IsValid)
        {
            throw new InvalidOperationException("Cannot convert a valid validation outcome to a failure result.");
        }

        return Result.Fail(Errors);
    }

    /// <summary>
    /// Determines whether this outcome is equal to another outcome.
    /// </summary>
    /// <param name="other">The other outcome.</param>
    /// <returns><see langword="true" /> if both outcomes are equal; otherwise, <see langword="false" />.</returns>
    public bool Equals(ValidationOutcome<T> other)
    {
        if (!Errors.Equals(other.Errors))
        {
            return false;
        }

        return Equals(Value, other.Value);
    }

    /// <inheritdoc />
    public override bool Equals(object? obj) => obj is ValidationOutcome<T> other && Equals(other);

    /// <inheritdoc />
    public override int GetHashCode() => HashCode.Combine(Value, Errors);

    /// <summary>
    /// Compares two validation outcomes for equality.
    /// </summary>
    public static bool operator ==(ValidationOutcome<T> left, ValidationOutcome<T> right) => left.Equals(right);

    /// <summary>
    /// Compares two validation outcomes for inequality.
    /// </summary>
    public static bool operator !=(ValidationOutcome<T> left, ValidationOutcome<T> right) => !left.Equals(right);
}
