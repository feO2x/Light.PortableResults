using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace Light.PortableResults.Validation;

/// <summary>
/// Provides factory members for <see cref="ValidatedValue{T}" />.
/// </summary>
public static class ValidatedValue
{
    /// <summary>
    /// Creates a successful validated value.
    /// </summary>
    /// <typeparam name="T">The validated value type.</typeparam>
    /// <param name="value">The non-null validated value.</param>
    /// <returns>The successful validated value.</returns>
    public static ValidatedValue<T> Success<T>(T value) => ValidatedValue<T>.Success(value);
}

/// <summary>
/// Represents a validated value produced by validator implementations.
/// Application code should use <c>Validate(...)</c> or <c>ValidateAsync(...)</c> and their
/// <see cref="Result{T}" /> return values instead of working with this type directly.
/// This type is public only because public validator base classes expose it in protected extensibility points.
/// </summary>
/// <typeparam name="T">The type of the validated value.</typeparam>
public readonly struct ValidatedValue<T> : IEquatable<ValidatedValue<T>>
{
    private readonly T? _value;

    private ValidatedValue(T value)
    {
        _value = value ?? throw new ArgumentNullException(nameof(value));
        HasValue = true;
    }

    /// <summary>
    /// Gets the explicit "no validated value was produced" state.
    /// </summary>
    public static ValidatedValue<T> NoValue => default;

    /// <summary>
    /// Creates a successful validated value.
    /// </summary>
    /// <param name="value">The non-null validated value.</param>
    /// <returns>The successful validated value.</returns>
    public static ValidatedValue<T> Success(T value) => new (value);

    /// <summary>
    /// Gets a value indicating whether a validated value was produced.
    /// </summary>
    [MemberNotNullWhen(true, nameof(Value), nameof(_value))]
    public bool HasValue { get; }

    /// <summary>
    /// Gets the validated value.
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown when no validated value is present.</exception>
    public T Value =>
        HasValue ?
            _value! :
            throw new InvalidOperationException("Cannot access Value when no validated value is present.");

    /// <summary>
    /// Attempts to get the validated value.
    /// </summary>
    /// <param name="value">The validated value when present; otherwise, the default value.</param>
    /// <returns><see langword="true" /> when a value is present; otherwise, <see langword="false" />.</returns>
    [MemberNotNullWhen(true, nameof(Value))]
    public bool TryGetValue([NotNullWhen(true)] out T? value)
    {
        if (HasValue)
        {
            value = _value;
            return true;
        }

        value = default;
        return false;
    }

    /// <summary>
    /// Determines whether this validated value is equal to another validated value.
    /// </summary>
    /// <param name="other">The other validated value.</param>
    /// <returns><see langword="true" /> when both instances are equal; otherwise, <see langword="false" />.</returns>
    public bool Equals(ValidatedValue<T> other) =>
        HasValue == other.HasValue &&
        EqualityComparer<T?>.Default.Equals(_value, other._value);

    /// <inheritdoc />
    public override bool Equals(object? obj) => obj is ValidatedValue<T> other && Equals(other);

    /// <inheritdoc />
    public override int GetHashCode() => HashCode.Combine(_value, HasValue);

    /// <summary>
    /// Compares two validated values for equality.
    /// </summary>
    public static bool operator ==(ValidatedValue<T> left, ValidatedValue<T> right) => left.Equals(right);

    /// <summary>
    /// Compares two validated values for inequality.
    /// </summary>
    public static bool operator !=(ValidatedValue<T> left, ValidatedValue<T> right) => !left.Equals(right);
}
