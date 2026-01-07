using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Light.Results.Metadata;

namespace Light.Results;

/// <summary>
/// Represents either a successful value of <typeparamref name="T" /> or one or more errors.
/// </summary>
[DebuggerDisplay("{DebuggerDisplay,nq}")]
public readonly struct Result<T>
{
    // Field order optimized to reduce padding: reference types first, then value types
    private readonly Errors _errors;
    private readonly MetadataObject? _metadata;
    private readonly T? _value;

    [MemberNotNullWhen(true, nameof(Value), nameof(_value))]
    public bool IsSuccess => _errors.Count == 0;

    [MemberNotNullWhen(false, nameof(Value), nameof(_value))]
    public bool IsFailure => !IsSuccess;

    /// <summary>Gets the successful value. Throws if this is a failure.</summary>
    public T Value =>
        IsSuccess ? _value : throw new InvalidOperationException("Cannot access Value on a failed Result.");

    /// <summary>Returns the errors collection (empty struct on success).</summary>
    public Errors Errors => _errors;

    /// <summary>Returns the first error (or throws if success).</summary>
    public Error FirstError => IsFailure ?
        _errors.First :
        throw new InvalidOperationException("Cannot access errors on a successful Result.");

    /// <summary>Gets the result-level metadata (correlation IDs, timing data, etc.).</summary>
    public MetadataObject? Metadata => _metadata;

    private Result(T value, MetadataObject? metadata = null)
    {
        _value = value;
        _errors = default;
        _metadata = metadata;
    }

    private Result(Errors errors, MetadataObject? metadata = null)
    {
        _value = default;
        _errors = errors;
        _metadata = metadata;
    }

    public static Result<T> Ok(T value) => new (value);

    public static Result<T> Ok(T value, MetadataObject metadata) => new (value, metadata);

    public static Result<T> Fail(Error error) => new (new Errors(error));

    public static Result<T> Fail(ReadOnlyMemory<Error> errors) =>
        !errors.IsEmpty ?
            new Result<T>(new Errors(errors)) :
            throw new ArgumentException("At least one error is required.", nameof(errors));

    /// <summary>Transforms the successful value.</summary>
    public Result<TOut> Map<TOut>(Func<T, TOut> map) =>
        IsSuccess ? Result<TOut>.Ok(map(Value), _metadata) : Result<TOut>.Fail(_errors, _metadata);

    /// <summary>Chains another result-returning function.</summary>
    public Result<TOut> Bind<TOut>(Func<T, Result<TOut>> bind)
    {
        if (!IsSuccess)
        {
            return Result<TOut>.Fail(_errors, _metadata);
        }

        var inner = bind(Value);
        // Merge metadata from this result into the bound result using MergeIfNeeded
        var merged = MetadataObjectExtensions.MergeIfNeeded(inner._metadata, _metadata);
        // Skip creating new result if metadata didn't change
        if (merged is null && inner._metadata is null)
        {
            return inner;
        }

        if (merged is not null && inner._metadata is not null && merged.Value == inner._metadata.Value)
        {
            return inner;
        }

        return inner.WithMetadata(merged);
    }

    /// <summary>Executes an action on success and returns the same result.</summary>
    public Result<T> Tap(Action<T> action)
    {
        if (IsSuccess)
        {
            action(Value);
        }

        return this;
    }

    /// <summary>Executes an action on failure and returns the same result.</summary>
    public Result<T> TapError(Action<Errors> action)
    {
        if (IsFailure)
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
        if (IsSuccess)
        {
            value = Value;
            return true;
        }

        value = default;
        return false;
    }

    public override string ToString() =>
        IsSuccess ? $"Ok({Value})" : $"Fail({string.Join(", ", _errors.Select(e => e.Message))})";

    private string DebuggerDisplay => IsSuccess ? $"Ok({Value})" : $"Fail({_errors.Count} error(s))";

    // Convenience implicit conversions
    public static implicit operator Result<T>(T value) => Ok(value);
    public static implicit operator Result<T>(Error error) => Fail(error);


    /// <summary>Creates a new result with the specified metadata.</summary>
    public Result<T> WithMetadata(MetadataObject metadata) =>
        IsSuccess ? new Result<T>(Value, metadata) : new Result<T>(_errors, metadata);

    /// <summary>Creates a new result with the specified metadata (or clears metadata if null).</summary>
    private Result<T> WithMetadata(MetadataObject? metadata) =>
        IsSuccess ? new Result<T>(Value, metadata) : new Result<T>(_errors, metadata);

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
public readonly struct Result
{
    private readonly Result<Unit> _inner;
    private Result(Result<Unit> inner) => _inner = inner;

    public bool IsSuccess => _inner.IsSuccess;
    public bool IsFailure => _inner.IsFailure;
    public Errors Errors => _inner.Errors;

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
}
