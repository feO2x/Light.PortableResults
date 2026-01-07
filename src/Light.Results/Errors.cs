using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;

namespace Light.Results;

public readonly struct Errors : IEnumerable<Error>
{
    private readonly Error _one;
    private readonly Error[]? _many;

    public int Count { get; }

    public Error First => Count switch
    {
        <= 0 => throw new InvalidOperationException("No errors present."),
        1 => _one,
        _ => _many![0]
    };

    public Errors(Error one)
    {
        _one = one;
        _many = null;
        Count = 1;
    }

    private Errors(Error[] many)
    {
        if (many is null)
        {
            throw new ArgumentNullException(nameof(many));
        }

        if (many.Length < 2)
        {
            throw new ArgumentException("Use single-error constructor for one error.", nameof(many));
        }

        _one = default;
        _many = many;
        Count = many.Length;
    }

    public static Errors FromArray(Error[] errors) => errors.Length == 1 ? new Errors(errors[0]) : new Errors(errors);

    public ImmutableArray<Error> ToImmutableArray() =>
        Count switch
        {
            0 => ImmutableArray<Error>.Empty,
            1 => [_one],
            _ => [.._many!]
        };

    public IEnumerator<Error> GetEnumerator()
    {
        switch (Count)
        {
            case 1:
                yield return _one;
                yield break;
            case > 1:
            {
                for (var i = 0; i < _many!.Length; i++)
                {
                    yield return _many[i];
                }

                break;
            }
        }
    }

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}
