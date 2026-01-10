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
public readonly struct Result<T> : IEquatable<Result<T>>
{
    private readonly Errors _errors;
    private readonly MetadataObject? _metadata;
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
        _metadata = metadata;
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
        if (errors.IsDefaultInstance)
        {
            throw new ArgumentException($"{nameof(errors)} must contain at least one error", nameof(errors));
        }

        _value = default;
        _errors = errors;
        _metadata = metadata;
    }

    /// <summary>
    /// Gets the value indicating whether this instance is a success.
    /// </summary>
    [MemberNotNullWhen(true, nameof(Value), nameof(_value))]
    public bool IsValid => _value is not null && _errors.Count is 0;

    /// <summary>
    /// Gets the successful value. Throws if this is a failure.
    /// </summary>
    public T Value =>
        IsValid ? _value : throw new InvalidOperationException("Cannot access Value on a failed Result.");

    /// <summary>Returns the errors collection (empty struct on success).</summary>
    public Errors Errors => _errors;

    /// <summary>Returns the first error (or throws if success).</summary>
    public Error FirstError => !IsValid ?
        _errors.First :
        throw new InvalidOperationException("Cannot access errors on a successful Result.");

    /// <summary>Gets the result-level metadata (correlation IDs, timing data, etc.).</summary>
    public MetadataObject? Metadata => _metadata;

    public string DebuggerDisplay => IsValid ? $"Ok({Value})" : $"Fail({_errors.Count} error(s))";

    public static Result<T> Ok(T value) => new (value);

    public static Result<T> Ok(T value, MetadataObject metadata) => new (value, metadata);

    public static Result<T> Fail(Error singleError) => new (new Errors(singleError));

    public static Result<T> Fail(ReadOnlyMemory<Error> manyErrors) => new (new Errors(manyErrors));

    /// <summary>Transforms the successful value.</summary>
    public Result<TOut> Map<TOut>(Func<T, TOut> map) =>
        IsValid ? Result<TOut>.Ok(map(Value), _metadata) : Result<TOut>.Fail(_errors, _metadata);

    /// <summary>Chains another result-returning function.</summary>
    public Result<TOut> Bind<TOut>(Func<T, Result<TOut>> bind)
    {
        if (!IsValid)
        {
            return Result<TOut>.Fail(_errors, _metadata);
        }

        var newResult = bind(Value);

        // Merge metadata from this result into the bound result using MergeIfNeeded
        var mergedMetadata = MetadataObjectExtensions.MergeIfNeeded(newResult._metadata, _metadata);

        // Skip creating new result if metadata didn't change
        if (mergedMetadata is null && newResult._metadata is null)
        {
            return newResult;
        }

        if (mergedMetadata is not null &&
            newResult._metadata is not null &&
            mergedMetadata.Value == newResult._metadata.Value)
        {
            return newResult;
        }

        return newResult.WithMetadata(mergedMetadata);
    }

    /// <summary>Executes an action on success and returns the same result.</summary>
    public Result<T> Tap(Action<T> action)
    {
        if (IsValid)
        {
            action(Value);
        }

        return this;
    }

    /// <summary>Executes an action on failure and returns the same result.</summary>
    public Result<T> TapError(Action<Errors> action)
    {
        if (!IsValid)
        {
            action(_errors);
        }

        return this;
    }

    /// <summary>
    /// Attempts to get the value if this is a success result.
    /// </summary>
    /// <param name="value">The value if successful; otherwise, default.</param>
    /// <returns>True if successful; false if this is a failure result.</returns>
    [MemberNotNullWhen(true, nameof(Value))]
    public bool TryGetValue([MaybeNullWhen(false)] out T value)
    {
        if (IsValid)
        {
            value = Value;
            return true;
        }

        value = default;
        return false;
    }

    public override string ToString() =>
        IsValid ? $"Ok({Value})" : $"Fail({string.Join(", ", _errors.Select(e => e.Message))})";

    public bool Equals(Result<T> other) => Equals(other, compareMetadata: true, valueComparer: null);

    public bool Equals(Result<T> other, bool compareMetadata, IEqualityComparer<T?>? valueComparer = null)
    {
        valueComparer ??= EqualityComparer<T?>.Default;

        if (!_errors.Equals(other._errors))
        {
            return false;
        }

        if (!valueComparer.Equals(_value, other._value))
        {
            return false;
        }

        if (compareMetadata)
        {
            return _metadata is null ?
                other._metadata is null :
                other._metadata is not null && _metadata.Equals(other._metadata);
        }

        return true;
    }

    public override bool Equals(object? obj) => obj is Result<T> other && Equals(other);

    public override int GetHashCode() => GetHashCode(includeMetadata: true);

    public int GetHashCode(bool includeMetadata, IEqualityComparer<T?>? equalityComparer = null)
    {
        equalityComparer ??= EqualityComparer<T?>.Default;
        var hash = new HashCode();
        hash.Add(_errors);
        hash.Add(_value, equalityComparer);
        if (_metadata is not null && includeMetadata)
        {
            hash.Add(_metadata);
        }

        return hash.ToHashCode();
    }

    // Convenience implicit conversions
    public static implicit operator Result<T>(T value) => new (value);
    public static implicit operator Result<T>(Error error) => Fail(error);
    public static implicit operator Result<T>(Errors errors) => new (errors);

    public static bool operator ==(Result<T> x, Result<T> y) => x.Equals(y);
    public static bool operator !=(Result<T> x, Result<T> y) => !(x == y);


    /// <summary>Creates a new result with the specified metadata.</summary>
    public Result<T> WithMetadata(MetadataObject metadata) =>
        IsValid ? new Result<T>(Value, metadata) : new Result<T>(_errors, metadata);

    /// <summary>Creates a new result with the specified metadata (or clears metadata if null).</summary>
    private Result<T> WithMetadata(MetadataObject? metadata) =>
        IsValid ? new Result<T>(Value, metadata) : new Result<T>(_errors, metadata);

    /// <summary>Creates a new result with additional metadata properties.</summary>
    public Result<T> WithMetadata(params (string Key, MetadataValue Value)[] properties)
    {
        var newMetadata = _metadata?.With(properties) ?? MetadataObject.Create(properties);
        return WithMetadata(newMetadata);
    }

    /// <summary>Merges the specified metadata into this result's metadata.</summary>
    public Result<T> MergeMetadata(
        MetadataObject other,
        MetadataMergeStrategy strategy = MetadataMergeStrategy.AddOrReplace
    )
    {
        var merged = MetadataObjectExtensions.MergeIfNeeded(_metadata, other, strategy);
        // Skip creating new result if metadata didn't change
        if (merged is null && _metadata is null)
        {
            return this;
        }

        if (merged is not null && _metadata is not null && merged.Value == _metadata.Value)
        {
            return this;
        }

        return WithMetadata(merged);
    }

    // Allow Result<T>.Fail(_errors) reuse without re-allocating arrays.
    private static Result<T> Fail(Errors errors, MetadataObject? metadata = null) => new (errors, metadata);

    private static Result<T> Ok(T value, MetadataObject? metadata) => new (value, metadata);
}

/// <summary>
/// Non-generic convenience result (success/failure only).
/// </summary>
[DebuggerDisplay("{DebuggerDisplay,nq}")]
public readonly struct Result : IEquatable<Result>
{
    private readonly Result<Unit> _inner;

    public Result() : this(Result<Unit>.Ok(Unit.Value)) { }
    private Result(Result<Unit> inner) => _inner = inner;

    public bool IsValid => _inner.IsValid;
    public Errors Errors => _inner.Errors;
    public Error FirstError => _inner.FirstError;
    public string DebuggerDisplay => _inner.DebuggerDisplay;

    /// <summary>Gets the result-level metadata (correlation IDs, timing data, etc.).</summary>
    public MetadataObject? Metadata => _inner.Metadata;

    public static Result Ok() => new (Result<Unit>.Ok(Unit.Value));
    public static Result Ok(MetadataObject metadata) => new (Result<Unit>.Ok(Unit.Value, metadata));
    public static Result Fail(Error error) => new (Result<Unit>.Fail(error));
    public static Result Fail(ReadOnlyMemory<Error> errors) => new (Result<Unit>.Fail(errors));

    /// <summary>Creates a new result with the specified metadata.</summary>
    public Result WithMetadata(MetadataObject metadata) => new (_inner.WithMetadata(metadata));

    /// <summary>Creates a new result with additional metadata properties.</summary>
    public Result WithMetadata(params (string Key, MetadataValue Value)[] properties) =>
        new (_inner.WithMetadata(properties));

    /// <summary>Merges the specified metadata into this result's metadata.</summary>
    public Result MergeMetadata(
        MetadataObject other,
        MetadataMergeStrategy strategy = MetadataMergeStrategy.AddOrReplace
    ) =>
        new (_inner.MergeMetadata(other, strategy));

    public bool Equals(Result other) => _inner.Equals(other._inner);

    public bool Equals(Result other, bool compareMetadata) => _inner.Equals(other._inner, compareMetadata);

    public override bool Equals(object? obj) => obj is Result other && _inner.Equals(other._inner);

    public override int GetHashCode() => _inner.GetHashCode();

    public int GetHashCode(bool includeMetadata) => _inner.GetHashCode(includeMetadata);

    public static bool operator ==(Result x, Result y) => x.Equals(y);
    public static bool operator !=(Result x, Result y) => !(x == y);
}
