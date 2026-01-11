using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Light.Results;

/// <summary>
/// Stores one or more errors with small-buffer optimization.
/// A single error is stored inline; multiple errors use a <see cref="ReadOnlyMemory{T}" /> instance.
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

    /// <summary>
    /// Gets the count of the errors.
    /// </summary>
    public int Count { get; }

    /// <summary>
    /// Gets the error at the specified index.
    /// </summary>
    /// <param name="index">The index of the error to get.</param>
    /// <returns>The error at the specified index.</returns>
    /// <exception cref="IndexOutOfRangeException">Thrown when <paramref name="index" /> is out of range.</exception>
    public Error this[int index] =>
        Count != 1 ? _manyErrors.Span[index] :
        index == 0 ? _singleError : throw new IndexOutOfRangeException();

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

    /// <summary>
    /// Gets an enumerator that iterates through the errors.
    /// This method is optimized, prefer calling it over <see cref="IEnumerable{T}.GetEnumerator()" />
    /// or <see cref="IEnumerable.GetEnumerator()" />.
    /// </summary>
    /// <returns>The enumerator.</returns>
    public Enumerator GetEnumerator() => new (this);

    /// <summary>
    /// Gets an enumerator that iterates through the errors. This method is not optimized, prefer to call the overload
    /// that returns a struct.
    /// </summary>
    /// <returns>The enumerator.</returns>
    IEnumerator<Error> IEnumerable<Error>.GetEnumerator() => GetEnumerator();

    /// <summary>
    /// Gets an enumerator that iterates through the errors. This method is not optimized, prefer to call the overload
    /// that returns a struct.
    /// </summary>
    /// <returns>The enumerator.</returns>
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    /// <summary>
    /// Gets the value indicating whether this instance is equal to the specified instance.
    /// This equality check is based on the count and the content of the errors. Two <see cref="Errors" /> instances
    /// are considered equal when they contain the same errors in exact the same order.
    /// </summary>
    /// <param name="other">The instance to compare to.</param>
    /// <returns>The value indicating whether this instance is equal to the specified instance.</returns>
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

    public bool Equals(Errors other, bool compareMetadata)
    {
        if (compareMetadata)
        {
            return Equals(other);
        }

        if (Count != other.Count)
        {
            return false;
        }

        return Count switch
        {
            0 => true,
            1 => _singleError.Equals(other._singleError, compareMetadata: false),
            _ => SequenceEqualWithoutMetadata(
                ref MemoryMarshal.GetReference(_manyErrors.Span),
                ref MemoryMarshal.GetReference(other._manyErrors.Span),
                Count
            )
        };
    }

    // ReSharper disable once CognitiveComplexity -- optimized code derived from .NET internal SpanHelpers class
    private static bool SequenceEqualWithoutMetadata(ref Error first, ref Error second, int length)
    {
        if (Unsafe.AreSame(ref first, ref second))
        {
            goto Equal;
        }

        var index = (IntPtr) 0; // Use IntPtr for arithmetic to avoid unnecessary 64->32->64 truncations
        while (length >= 8)
        {
            length -= 8;

            if (!Unsafe.Add(ref first, index).Equals(Unsafe.Add(ref second, index), compareMetadata: false))
            {
                goto NotEqual;
            }

            if (!Unsafe.Add(ref first, index + 1).Equals(Unsafe.Add(ref second, index + 1), compareMetadata: false))
            {
                goto NotEqual;
            }

            if (!Unsafe.Add(ref first, index + 2).Equals(Unsafe.Add(ref second, index + 2), compareMetadata: false))
            {
                goto NotEqual;
            }

            if (!Unsafe.Add(ref first, index + 3).Equals(Unsafe.Add(ref second, index + 3), compareMetadata: false))
            {
                goto NotEqual;
            }

            if (!Unsafe.Add(ref first, index + 4).Equals(Unsafe.Add(ref second, index + 4), compareMetadata: false))
            {
                goto NotEqual;
            }

            if (!Unsafe.Add(ref first, index + 5).Equals(Unsafe.Add(ref second, index + 5), compareMetadata: false))
            {
                goto NotEqual;
            }

            if (!Unsafe.Add(ref first, index + 6).Equals(Unsafe.Add(ref second, index + 6), compareMetadata: false))
            {
                goto NotEqual;
            }

            if (!Unsafe.Add(ref first, index + 7).Equals(Unsafe.Add(ref second, index + 7), compareMetadata: false))
            {
                goto NotEqual;
            }

            index += 8;
        }

        if (length >= 4)
        {
            length -= 4;

            if (!Unsafe.Add(ref first, index).Equals(Unsafe.Add(ref second, index), compareMetadata: false))
            {
                goto NotEqual;
            }

            if (!Unsafe.Add(ref first, index + 1).Equals(Unsafe.Add(ref second, index + 1), compareMetadata: false))
            {
                goto NotEqual;
            }

            if (!Unsafe.Add(ref first, index + 2).Equals(Unsafe.Add(ref second, index + 2), compareMetadata: false))
            {
                goto NotEqual;
            }

            if (!Unsafe.Add(ref first, index + 3).Equals(Unsafe.Add(ref second, index + 3), compareMetadata: false))
            {
                goto NotEqual;
            }

            index += 4;
        }

        while (length > 0)
        {
            if (!Unsafe.Add(ref first, index).Equals(Unsafe.Add(ref second, index), compareMetadata: false))
            {
                goto NotEqual;
            }

            index += 1;
            length--;
        }

        Equal:
        return true;

        NotEqual: // Workaround for https://github.com/dotnet/coreclr/issues/13549
        return false;
    }

    /// <summary>
    /// Gets the value indicating whether this instance is equal to the specified instance.
    /// Calls into <see cref="Equals(Errors)" /> when possible.
    /// </summary>
    /// <param name="obj">The instance to compare to.</param>
    /// <returns>The value indicating whether this instance is equal to the specified instance.</returns>
    public override bool Equals(object? obj) => obj is Errors other && Equals(other);

    /// <summary>
    /// Gets the hash code of this instance.
    /// </summary>
    /// <returns>The hash code of this instance.</returns>
    public override int GetHashCode()
    {
        switch (Count)
        {
            case 0: return 0;
            case 1: return _singleError.GetHashCode();
            default:
                var hash = new HashCode();
                var span = _manyErrors.Span;
                for (var i = 0; i < span.Length; i++)
                {
                    hash.Add(span[i]);
                }

                return hash.ToHashCode();
        }
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

        /// <summary>
        /// Initializes a new instance of <see cref="Enumerator" />.
        /// </summary>
        /// <param name="errors">The errors to enumerate.</param>
        public Enumerator(Errors errors)
        {
            _one = errors._singleError;
            _many = errors._manyErrors;
            _count = errors.Count;
            _index = -1;
        }

        /// <summary>
        /// Gets the current element in the collection.
        /// </summary>
        /// <exception cref="InvalidOperationException">
        /// Thrown when the enumerator is positioned before the first element or after the last element.
        /// </exception>
        public Error Current => _count == 1 ?
            _index == 0 ?
                _one :
                throw new InvalidOperationException(
                    "Enumerator is positioned before the first element or after the last element."
                ) :
            _many.Span[_index];

        /// <summary>
        /// Gets the current element in the collection.
        /// </summary>
        object IEnumerator.Current => Current;

        /// <summary>
        /// Advances the enumerator to the next element of the collection.
        /// </summary>
        /// <returns>
        /// <see langword="true" /> if the enumerator was successfully advanced to the next element;
        /// <see langword="false" /> if the enumerator has passed the end of the collection.
        /// </returns>
        public bool MoveNext()
        {
            _index++;
            return _index < _count;
        }

        /// <summary>
        /// Sets the enumerator to its initial position, which is before the first element in the collection.
        /// </summary>
        public void Reset() => _index = -1;

        /// <summary>
        /// Does nothing, this enumerator has no underlying resources.
        /// </summary>
        public void Dispose() { }
    }
}
