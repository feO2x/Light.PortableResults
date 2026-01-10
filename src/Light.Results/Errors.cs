using System;
using System.Collections;
using System.Collections.Generic;

namespace Light.Results;

/// <summary>
/// Stores one or more errors with small-buffer optimization.
/// Single error is stored inline; multiple errors use a <see cref="ReadOnlyMemory{T}" />.
/// Implements <see cref="IReadOnlyList{T}" /> with a zero-allocation value-type enumerator.
/// </summary>
public readonly struct Errors : IReadOnlyList<Error>, IEquatable<Errors>
{
    private readonly Error _singleError;
    private readonly ReadOnlyMemory<Error> _manyErrors;

    /// <summary>
    /// Initializes a new instance of <see cref="Errors" />, containing a single error instance.
    /// </summary>
    /// <param name="singleError">The error that is stored inline.</param>
    /// <exception cref="ArgumentException">Thrown when <paramref name="singleError" /> is the default instance.</exception>
    public Errors(Error singleError)
    {
        if (singleError.IsDefaultInstance)
        {
            throw new ArgumentException($"'{nameof(singleError)}' must not be default instance", nameof(singleError));
        }

        _singleError = singleError;
        _manyErrors = ReadOnlyMemory<Error>.Empty;
        Count = 1;
    }

    /// <summary>
    /// Initializes a new instance of <see cref="Errors" />, containing one or more errors.
    /// If only one error is contained in the <paramref name="manyErrors" /> parameter, it is stored inline.
    /// </summary>
    /// <param name="manyErrors">The collection containing many errors.</param>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="manyErrors" /> is empty or contains at least one error instance which is the default instance.
    /// </exception>
    public Errors(ReadOnlyMemory<Error> manyErrors)
    {
        switch (manyErrors.Length)
        {
            case 0:
                throw new ArgumentException(
                    $"'{nameof(manyErrors)}' must contain one or more errors",
                    nameof(manyErrors)
                );
            case 1:
            {
                var singleError = manyErrors.Span[0];
                if (singleError.IsDefaultInstance)
                {
                    throw new ArgumentException(
                        $"The single error in '{nameof(manyErrors)}' must not be the default instance",
                        nameof(manyErrors)
                    );
                }

                _singleError = singleError;
                _manyErrors = ReadOnlyMemory<Error>.Empty;
                Count = 1;
                return;
            }
            default:
                var span = manyErrors.Span;
                for (var i = 0; i < span.Length; i++)
                {
                    if (span[i].IsDefaultInstance)
                    {
                        throw new ArgumentException(
                            $"The error at index {i} in '{nameof(manyErrors)}' must not be the default instance",
                            nameof(manyErrors)
                        );
                    }
                }

                _singleError = default;
                _manyErrors = manyErrors;
                Count = manyErrors.Length;
                return;
        }
    }

    public int Count { get; }

    public Error this[int index]
    {
        get
        {
            // Use unsigned comparison to fold the index < 0 and index >= Count checks into one branch.
            if ((uint) index >= (uint) Count)
            {
                throw new ArgumentOutOfRangeException(nameof(index));
            }

            return Count == 1 ? _singleError : _manyErrors.Span[index];
        }
    }

    public Error First => Count switch
    {
        0 => throw new InvalidOperationException("No errors present"),
        1 => _singleError,
        _ => _manyErrors.Span[0]
    };

    /// <summary>
    /// Gets the value indicating whether this instance is the default instance.
    /// </summary>
    public bool IsDefaultInstance => Count == 0;

    public Enumerator GetEnumerator() => new (this);

    IEnumerator<Error> IEnumerable<Error>.GetEnumerator() => GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    public bool Equals(Errors other)
    {
        if (Count != other.Count)
        {
            return false;
        }

        return Count switch
        {
            0 => true,
            1 => _singleError.Equals(other._singleError),
            _ => _manyErrors.Span.SequenceEqual(other._manyErrors.Span)
        };
    }

    public override bool Equals(object? obj) => obj is Errors other && Equals(other);

    public override int GetHashCode()
    {
        if (Count == 0)
        {
            return 0;
        }

        var hash = new HashCode();

        if (Count == 1)
        {
            hash.Add(_singleError);
            return hash.ToHashCode();
        }

        var span = _manyErrors.Span;
        for (var i = 0; i < span.Length; i++)
        {
            hash.Add(span[i]);
        }

        return hash.ToHashCode();
    }

    public static bool operator ==(Errors left, Errors right) => left.Equals(right);

    public static bool operator !=(Errors left, Errors right) => !left.Equals(right);

    /// <summary>
    /// Value-type enumerator that avoids compiler-generated state machine allocations.
    /// </summary>
    public struct Enumerator : IEnumerator<Error>
    {
        private readonly Error _one;
        private readonly ReadOnlyMemory<Error> _many;
        private readonly int _count;
        private int _index;

        public Enumerator(Errors errors)
        {
            _one = errors._singleError;
            _many = errors._manyErrors;
            _count = errors.Count;
            _index = -1;
        }

        public Error Current => _count == 1 ?
            _index == 0 ?
                _one :
                throw new InvalidOperationException(
                    "Enumerator is positioned before the first element or after the last element."
                ) :
            _many.Span[_index];

        object IEnumerator.Current => Current;

        public bool MoveNext()
        {
            _index++;
            return _index < _count;
        }

        public void Reset() => _index = -1;

        public void Dispose() { }
    }
}
