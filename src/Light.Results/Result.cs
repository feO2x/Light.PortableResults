using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Light.Results.Metadata;

namespace Light.Results;

/// <summary>
/// Represents either a successful value of <typeparamref name="T" /> or one or more errors.
/// </summary>
[DebuggerDisplay("{DebuggerDisplay,nq}")]
public readonly struct Result<T> : IResultObject<T>, IEquatable<Result<T>>, ICanReplaceMetadata<Result<T>>
{
    private readonly Errors _errors;
    private readonly T? _value;

    /// <summary>
    /// Initializes a new instance of <see cref="Result{T}" /> with a successful value.
    /// </summary>
    /// <param name="value">The non-null value.</param>
    /// <param name="metadata">The optional metadata for this result.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="value" /> is null.</exception>
    // ReSharper disable once MemberCanBePrivate.Global -- public API
    public Result(T value, MetadataObject? metadata = null)
    {
        _value = value ?? throw new ArgumentNullException(nameof(value));
        _errors = default;
        Metadata = metadata;
    }

    /// <summary>
    /// Initializes a new instance of <see cref="Result{T}" /> with one or more errors.
    /// </summary>
    /// <param name="errors">The errors of this result instance. At least one error must be present.</param>
    /// <param name="metadata">The optional metadata for this result.</param>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="errors" /> is the default instance and thus contains no errors.
    /// </exception>
    // ReSharper disable once MemberCanBePrivate.Global -- public API
    public Result(Errors errors, MetadataObject? metadata = null)
    {
        if (errors.IsEmpty)
        {
            throw new ArgumentException($"{nameof(errors)} must contain at least one error", nameof(errors));
        }

        _value = default;
        _errors = errors;
        Metadata = metadata;
    }

    /// <summary>
    /// Gets the value indicating whether this instance is a success.
    /// </summary>
    [MemberNotNullWhen(true, nameof(Value), nameof(_value))]
    public bool IsValid => _value is not null && _errors.Count is 0;

    /// <summary>
    /// Gets the successful value. Throws if this is a failure.
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown when this result contains errors.</exception>
    public T Value =>
        IsValid ? _value : throw new InvalidOperationException("Cannot access Value on a failed Result.");

    /// <summary>
    /// Returns the errors collection (empty struct on success).
    /// </summary>
    public Errors Errors => _errors;

    /// <summary>
    /// Gets the value indicating whether this instance has a value. Always returns true.
    /// </summary>
    public bool HasValue => true;

    /// <summary>
    /// Returns the first error (or throws if result contains no errors).
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown when this result contains no errors.</exception>
    public Error FirstError => !IsValid ?
        _errors.First :
        throw new InvalidOperationException("Cannot access errors on a successful Result.");

    /// <summary>
    /// Gets the result-level metadata.
    /// </summary>
    public MetadataObject? Metadata { get; }

    /// <summary>
    /// Provides a string representation for the debugger.
    /// </summary>
    public string DebuggerDisplay =>
        IsValid ? $"OK('{Value}')" :
            _errors.Count is 1 ?
                $"Fail(single error: '{FirstError.Message}')" :
                $"Fail({_errors.Count.ToString()} errors)";

    /// <summary>
    /// Creates a successful result with the specified value and optional metadata.
    /// </summary>
    /// <param name="value">The value of the successful result.</param>
    /// <param name="metadata">The optional metadata.</param>
    /// <returns>The successful result.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="value" /> is null.</exception>
    public static Result<T> Ok(T value, MetadataObject? metadata = null) => new (value, metadata);

    /// <summary>
    /// Creates a failed result with a single error and optional metadata.
    /// </summary>
    /// <param name="singleError">The error of the failed result.</param>
    /// <param name="metadata">The optional metadata.</param>
    /// <returns>The failed result.</returns>
    public static Result<T> Fail(Error singleError, MetadataObject? metadata = null) =>
        new (new Errors(singleError), metadata);

    /// <summary>
    /// Creates a failed result with multiple errors and optional metadata.
    /// </summary>
    /// <param name="manyErrors">The errors of the failed result.</param>
    /// <param name="metadata">The optional metadata.</param>
    /// <returns>The failed result.</returns>
    public static Result<T> Fail(ReadOnlyMemory<Error> manyErrors, MetadataObject? metadata = null) =>
        new (new Errors(manyErrors), metadata);

    /// <summary>
    /// Creates a failed result with the specified errors and optional metadata.
    /// </summary>
    /// <param name="errors">The errors of the failed result.</param>
    /// <param name="metadata">The optional metadata.</param>
    /// <returns>The failed result.</returns>
    public static Result<T> Fail(Errors errors, MetadataObject? metadata = null) => new (errors, metadata);

    /// <summary>
    /// Attempts to get the value if this is a valid result.
    /// </summary>
    /// <param name="value">The value if successful; otherwise, default.</param>
    /// <returns>True if result is valid; false if this is an erroneous result.</returns>
    [MemberNotNullWhen(true, nameof(Value))]
    public bool TryGetValue([MaybeNullWhen(false)] out T value)
    {
        if (IsValid)
        {
            value = _value;
            return true;
        }

        value = default;
        return false;
    }

    /// <summary>
    /// Returns a string representation of this result.
    /// </summary>
    public override string ToString() =>
        IsValid ? $"Ok({Value})" : $"Fail({string.Join(", ", _errors.Select(e => e.Message))})";

    /// <summary>
    /// Determines whether this result is equal to another result. This method compares metadata and uses the
    /// default Equality Comparer for values.
    /// </summary>
    /// <param name="other">The other result to compare.</param>
    /// <returns>True if the results are equal; otherwise, false.</returns>
    public bool Equals(Result<T> other) => Equals(other, compareMetadata: true, valueComparer: null);

    /// <summary>
    /// Determines whether this result is equal to another result with options to compare metadata and specify a value comparer.
    /// </summary>
    /// <param name="other">The other result to compare.</param>
    /// <param name="compareMetadata">Whether to compare metadata.</param>
    /// <param name="valueComparer">The optional equality comparer for values.</param>
    /// <returns>True if the results are equal; otherwise, false.</returns>
    public bool Equals(Result<T> other, bool compareMetadata, IEqualityComparer<T?>? valueComparer = null)
    {
        valueComparer ??= EqualityComparer<T?>.Default;

        if (!_errors.Equals(other._errors, compareMetadata))
        {
            return false;
        }

        if (!valueComparer.Equals(_value, other._value))
        {
            return false;
        }

        if (compareMetadata)
        {
            return Metadata is null ?
                other.Metadata is null :
                other.Metadata is not null && Metadata.Value.Equals(other.Metadata.Value);
        }

        return true;
    }

    /// <summary>
    /// Determines whether this result is equal to the specified object.
    /// </summary>
    /// <param name="obj">The object to compare.</param>
    /// <returns>True if the object is a result and is equal to this result; otherwise, false.</returns>
    public override bool Equals(object? obj) => obj is Result<T> other && Equals(other);

    /// <summary>
    /// Returns the hash code for this result.
    /// </summary>
    public override int GetHashCode() => GetHashCode(includeMetadata: true);

    /// <summary>
    /// Returns the hash code for this result with options to include metadata and specify an equality comparer.
    /// </summary>
    /// <param name="includeMetadata">Whether to include metadata in the hash code calculation.</param>
    /// <param name="equalityComparer">The optional equality comparer for values.</param>
    /// <returns>The hash code.</returns>
    public int GetHashCode(bool includeMetadata, IEqualityComparer<T?>? equalityComparer = null)
    {
        equalityComparer ??= EqualityComparer<T?>.Default;
        var hash = new HashCode();
        hash.Add(_errors);
        hash.Add(_value, equalityComparer);
        if (Metadata is not null && includeMetadata)
        {
            hash.Add(Metadata);
        }

        return hash.ToHashCode();
    }

    /// <summary>
    /// Determines whether two results are equal.
    /// </summary>
    public static bool operator ==(Result<T> x, Result<T> y) => x.Equals(y);

    /// <summary>
    /// Determines whether two results are not equal.
    /// </summary>
    public static bool operator !=(Result<T> x, Result<T> y) => !(x == y);

    /// <summary>
    /// Creates a new instance of this result with the metadata being replaced by the specified one.
    /// Pass <c>null</c> to clear metadata.
    /// </summary>
    /// <param name="metadata">The metadata to use, or <c>null</c> to clear.</param>
    /// <returns>A new result instance with the specified metadata.</returns>
    public Result<T> ReplaceMetadata(MetadataObject? metadata) =>
        IsValid ? new Result<T>(Value, metadata) : new Result<T>(_errors, metadata);
}

/// <summary>
/// <para>
/// Represents either a successful operation or one or more errors.
/// </para>
/// <para>
/// This is a convenience type for <see cref="Result{T}" /> with <see cref="Unit" /> as the value type.
/// </para>
/// </summary>
[DebuggerDisplay("{DebuggerDisplay,nq}")]
public readonly struct Result : IResultObject, IEquatable<Result>, ICanReplaceMetadata<Result>
{
    private readonly Result<Unit> _inner;

    /// <summary>
    /// Initializes a new instance of <see cref="Result" /> representing a successful operation.
    /// </summary>
    public Result() : this(Result<Unit>.Ok(Unit.Value)) { }

    private Result(Result<Unit> inner) => _inner = inner;

    /// <summary>
    /// Gets the value indicating whether this instance is a success.
    /// </summary>
    public bool IsValid => _inner.IsValid;

    /// <summary>Returns the errors collection (empty struct on success).</summary>
    public Errors Errors => _inner.Errors;

    /// <summary>
    /// Gets the value indicating whether this instance has a value. Always returns false.
    /// </summary>
    public bool HasValue => false;

    /// <summary>Returns the first error (or throws if result contains no errors).</summary>
    /// <exception cref="InvalidOperationException">Thrown when this result contains no errors.</exception>
    public Error FirstError => _inner.FirstError;

    /// <summary>
    /// Provides a string representation for the debugger.
    /// </summary>
    public string DebuggerDisplay => _inner.IsValid ? "OK" : _inner.DebuggerDisplay;

    /// <summary>Gets the result-level metadata.</summary>
    public MetadataObject? Metadata => _inner.Metadata;

    /// <summary>
    /// Creates a successful result with optional metadata.
    /// </summary>
    /// <param name="metadata">The optional metadata.</param>
    /// <returns>The successful result.</returns>
    public static Result Ok(MetadataObject? metadata = null) => new (Result<Unit>.Ok(Unit.Value, metadata));

    /// <summary>
    /// Creates a failed result with a single error and optional metadata.
    /// </summary>
    /// <param name="singleError">The error of the failed result.</param>
    /// <param name="metadata">The optional metadata.</param>
    /// <returns>The failed result.</returns>
    public static Result Fail(Error singleError, MetadataObject? metadata = null) =>
        new (Result<Unit>.Fail(singleError, metadata));

    /// <summary>
    /// Creates a failed result with multiple errors and optional metadata.
    /// </summary>
    /// <param name="errors">The errors of the failed result.</param>
    /// <param name="metadata">The optional metadata.</param>
    /// <returns>The failed result.</returns>
    public static Result Fail(ReadOnlyMemory<Error> errors, MetadataObject? metadata = null) =>
        new (Result<Unit>.Fail(errors, metadata));

    /// <summary>
    /// Creates a failed result with the specified errors and optional metadata.
    /// </summary>
    /// <param name="errors">The errors of the failed result.</param>
    /// <param name="metadata">The optional metadata.</param>
    /// <returns>The failed result.</returns>
    public static Result Fail(Errors errors, MetadataObject? metadata = null) =>
        new (Result<Unit>.Fail(errors, metadata));

    /// <summary>
    /// Creates a new instance of this result with the metadata being replaced by the specified one.
    /// Pass <c>null</c> to clear metadata.
    /// </summary>
    /// <param name="metadata">The metadata to use, or <c>null</c> to clear.</param>
    /// <returns>A new result instance with the specified metadata.</returns>
    public Result ReplaceMetadata(MetadataObject? metadata) => new (_inner.ReplaceMetadata(metadata));

    /// <summary>
    /// Determines whether this result is equal to another result.
    /// </summary>
    /// <param name="other">The other result to compare.</param>
    /// <returns>True if the results are equal; otherwise, false.</returns>
    public bool Equals(Result other) => _inner.Equals(other._inner);

    /// <summary>
    /// Determines whether this result is equal to another result with an option to compare metadata.
    /// </summary>
    /// <param name="other">The other result to compare.</param>
    /// <param name="compareMetadata">Whether to compare metadata.</param>
    /// <returns>True if the results are equal; otherwise, false.</returns>
    public bool Equals(Result other, bool compareMetadata) => _inner.Equals(other._inner, compareMetadata);

    /// <summary>
    /// Determines whether this result is equal to the specified object.
    /// </summary>
    /// <param name="obj">The object to compare.</param>
    /// <returns>True if the object is a result and is equal to this result; otherwise, false.</returns>
    public override bool Equals(object? obj) => obj is Result other && _inner.Equals(other._inner);

    /// <summary>
    /// Returns the hash code for this result.
    /// </summary>
    public override int GetHashCode() => _inner.GetHashCode();

    /// <summary>
    /// Returns the hash code for this result with an option to include metadata.
    /// </summary>
    /// <param name="includeMetadata">Whether to include metadata in the hash code calculation.</param>
    /// <returns>The hash code.</returns>
    public int GetHashCode(bool includeMetadata) => _inner.GetHashCode(includeMetadata);

    /// <summary>
    /// Determines whether two results are equal.
    /// </summary>
    public static bool operator ==(Result x, Result y) => x.Equals(y);

    /// <summary>
    /// Determines whether two results are not equal.
    /// </summary>
    public static bool operator !=(Result x, Result y) => !(x == y);

    /// <summary>
    /// Returns a string representation of this result.
    /// </summary>
    public override string ToString() =>
        IsValid ? "OK" : $"Fail({string.Join(", ", _inner.Errors.Select(e => e.Message))})";
}
