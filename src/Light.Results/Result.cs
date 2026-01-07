using System;
using System.Collections.Generic;
using System.Collections.Immutable;
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
    private readonly Errors _errors;
    private readonly MetadataObject? _metadata;

    [MemberNotNullWhen(true, nameof(Value))]
    public bool IsSuccess { get; }

    [MemberNotNullWhen(false, nameof(Value))]
    public bool IsFailure => !IsSuccess;

    /// <summary>Gets the successful value. Throws if this is a failure.</summary>
    public T Value =>
        IsSuccess ? field! : throw new InvalidOperationException("Cannot access Value on a failed Result.");

    /// <summary>Returns errors as an immutable array (empty on success).</summary>
    public ImmutableArray<Error> ErrorList => IsSuccess ? ImmutableArray<Error>.Empty : _errors.ToImmutableArray();

    /// <summary>Returns the first error (or throws if success).</summary>
    public Error FirstError => IsFailure ?
        _errors.First :
        throw new InvalidOperationException("Cannot access errors on a successful Result.");

    /// <summary>Gets the result-level metadata (correlation IDs, timing data, etc.).</summary>
    public MetadataObject? Metadata => _metadata;

    private Result(T value, MetadataObject? metadata = null)
    {
        IsSuccess = true;
        Value = value;
        _errors = default;
        _metadata = metadata;
    }

    private Result(Errors errors, MetadataObject? metadata = null)
    {
        IsSuccess = false;
        Value = default;
        _errors = errors;
        _metadata = metadata;
    }

    public static Result<T> Ok(T value) => new (value);

    public static Result<T> Ok(T value, MetadataObject metadata) => new (value, metadata);

    public static Result<T> Fail(Error error) => new (new Errors(error));

    public static Result<T> Fail(IEnumerable<Error> errors)
    {
        if (errors is null)
        {
            throw new ArgumentNullException(nameof(errors));
        }

        // Avoid multiple enumeration and keep it compact.
        if (errors is Error[] arr)
        {
            if (arr.Length == 0)
            {
                throw new ArgumentException("At least one error is required.", nameof(errors));
            }

            return new Result<T>(Errors.FromArray(arr));
        }

        var list = errors as IList<Error> ?? errors.ToList();
        if (list.Count == 0)
        {
            throw new ArgumentException("At least one error is required.", nameof(errors));
        }

        return list.Count == 1 ? Fail(list[0]) : new Result<T>(Errors.FromArray(list.ToArray()));
    }

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
        // Merge metadata from this result into the bound result
        return _metadata is null ?
            inner :
            inner._metadata is null ?
                inner.WithMetadata(_metadata.Value) :
                inner.MergeMetadata(_metadata.Value);
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
    public Result<T> TapError(Action<ImmutableArray<Error>> action)
    {
        if (IsFailure)
        {
            action(ErrorList);
        }

        return this;
    }

    public override string ToString()
        => IsSuccess ? $"Ok({Value})" : $"Fail({string.Join(", ", ErrorList.Select(e => e.Code))})";

    private string DebuggerDisplay => IsSuccess ? $"Ok({Value})" : $"Fail({ErrorList.Length} error(s))";

    // Convenience implicit conversions
    public static implicit operator Result<T>(T value) => Ok(value);
    public static implicit operator Result<T>(Error error) => Fail(error);


    /// <summary>Creates a new result with the specified metadata.</summary>
    public Result<T> WithMetadata(MetadataObject metadata)
    {
        if (IsSuccess)
        {
            return new Result<T>(Value, metadata);
        }

        return new Result<T>(_errors, metadata);
    }

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
        if (_metadata is null)
        {
            return WithMetadata(other);
        }

        var merged = _metadata.Value.Merge(other, strategy);
        return WithMetadata(merged);
    }

    // Allow Result<T>.Fail(_errors) reuse without re-allocating arrays.
    private static Result<T> Fail(Errors errors, MetadataObject? metadata = null) => new (errors, metadata);

    internal static Result<T> Ok(T value, MetadataObject? metadata) => new (value, metadata);
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
    public ImmutableArray<Error> ErrorList => _inner.ErrorList;

    /// <summary>Gets the result-level metadata (correlation IDs, timing data, etc.).</summary>
    public MetadataObject? Metadata => _inner.Metadata;

    public static Result Ok() => new (Result<Unit>.Ok(Unit.Value));
    public static Result Ok(MetadataObject metadata) => new (Result<Unit>.Ok(Unit.Value, metadata));
    public static Result Fail(Error error) => new (Result<Unit>.Fail(error));
    public static Result Fail(IEnumerable<Error> errors) => new (Result<Unit>.Fail(errors));

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

// Internal errors storage with small-buffer optimization:
// - one error inline (no array allocation)
// - many errors in an array
