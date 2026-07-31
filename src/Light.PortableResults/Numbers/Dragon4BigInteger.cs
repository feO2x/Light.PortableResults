// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;

namespace Light.PortableResults.Numbers;

// Fixed-buffer unsigned integer containing only the operations required by shortest-mode Dragon4.
internal unsafe ref struct Dragon4BigInteger
{
    // Shortest binary64 formatting needs fewer than 70 blocks. The additional headroom retains the
    // upstream implementation's simple fixed-buffer overflow invariant.
    private const int MaxBlockCount = 128;
    private const int BitsPerBlock = 32;

    private int _length;
    private fixed uint _blocks[MaxBlockCount];

    public int Length => _length;

    public bool IsZero => _length == 0;

    public uint GetBlock(int index)
    {
        Debug.Assert(index >= 0 && index < _length);
        return _blocks[index];
    }

    public static void SetUInt32(out Dragon4BigInteger result, uint value)
    {
        result._length = value == 0 ? 0 : 1;
        if (value != 0)
        {
            result._blocks[0] = value;
        }
    }

    public static void SetUInt64(out Dragon4BigInteger result, ulong value)
    {
        if (value <= uint.MaxValue)
        {
            SetUInt32(out result, (uint) value);
            return;
        }

        result._blocks[0] = (uint) value;
        result._blocks[1] = (uint) (value >> 32);
        result._length = 2;
    }

    public static void Pow2(uint exponent, out Dragon4BigInteger result)
    {
        var block = (int) (exponent >> 5);
        var bit = (int) (exponent & 31);
        Debug.Assert(block < MaxBlockCount);

        result._length = block + 1;
        for (var index = 0; index < block; index++)
        {
            result._blocks[index] = 0;
        }

        result._blocks[block] = 1U << bit;
    }

    public static void Pow10(uint exponent, out Dragon4BigInteger result)
    {
        SetUInt32(out var accumulator, 1);
        SetUInt32(out var factor, 10);

        while (exponent != 0)
        {
            if ((exponent & 1) != 0)
            {
                Multiply(ref accumulator, ref factor, out var product);
                Copy(out accumulator, ref product);
            }

            exponent >>= 1;
            if (exponent != 0)
            {
                Multiply(ref factor, ref factor, out var square);
                Copy(out factor, ref square);
            }
        }

        Copy(out result, ref accumulator);
    }

    public static void Add(
        scoped ref Dragon4BigInteger left,
        scoped ref Dragon4BigInteger right,
        out Dragon4BigInteger result
    )
    {
        ref var larger = ref left;
        ref var smaller = ref right;
        if (left._length < right._length)
        {
            larger = ref right;
            smaller = ref left;
        }

        var index = 0;
        ulong carry = 0;
        for (; index < smaller._length; index++)
        {
            var sum = carry + larger._blocks[index] + smaller._blocks[index];
            result._blocks[index] = (uint) sum;
            carry = sum >> BitsPerBlock;
        }

        for (; index < larger._length; index++)
        {
            var sum = carry + larger._blocks[index];
            result._blocks[index] = (uint) sum;
            carry = sum >> BitsPerBlock;
        }

        result._length = larger._length;
        if (carry != 0)
        {
            Debug.Assert(result._length < MaxBlockCount);
            result._blocks[result._length++] = (uint) carry;
        }
    }

    public static int Compare(
        scoped ref Dragon4BigInteger left,
        scoped ref Dragon4BigInteger right
    )
    {
        if (left._length != right._length)
        {
            return left._length > right._length ? 1 : -1;
        }

        for (var index = left._length - 1; index >= 0; index--)
        {
            if (left._blocks[index] != right._blocks[index])
            {
                return left._blocks[index] > right._blocks[index] ? 1 : -1;
            }
        }

        return 0;
    }

    public static void Multiply(
        scoped ref Dragon4BigInteger value,
        uint multiplier,
        out Dragon4BigInteger result
    )
    {
        if (multiplier == 0 || value._length == 0)
        {
            result._length = 0;
            return;
        }

        ulong carry = 0;
        var index = 0;
        for (; index < value._length; index++)
        {
            var product = (ulong) value._blocks[index] * multiplier + carry;
            result._blocks[index] = (uint) product;
            carry = product >> BitsPerBlock;
        }

        result._length = value._length;
        if (carry != 0)
        {
            Debug.Assert(result._length < MaxBlockCount);
            result._blocks[result._length++] = (uint) carry;
        }
    }

    public static void Multiply(
        scoped ref Dragon4BigInteger left,
        scoped ref Dragon4BigInteger right,
        out Dragon4BigInteger result
    )
    {
        if (left._length == 0 || right._length == 0)
        {
            result._length = 0;
            return;
        }

        var resultLength = left._length + right._length;
        Debug.Assert(resultLength <= MaxBlockCount);
        result._length = resultLength;
        for (var index = 0; index < resultLength; index++)
        {
            result._blocks[index] = 0;
        }

        for (var rightIndex = 0; rightIndex < right._length; rightIndex++)
        {
            ulong carry = 0;
            var resultIndex = rightIndex;
            for (var leftIndex = 0; leftIndex < left._length; leftIndex++, resultIndex++)
            {
                var product =
                    result._blocks[resultIndex] +
                    (ulong) left._blocks[leftIndex] * right._blocks[rightIndex] +
                    carry;
                result._blocks[resultIndex] = (uint) product;
                carry = product >> BitsPerBlock;
            }

            result._blocks[resultIndex] = (uint) carry;
        }

        while (result._length > 0 && result._blocks[result._length - 1] == 0)
        {
            result._length--;
        }
    }

    public static uint HeuristicDivide(
        scoped ref Dragon4BigInteger dividend,
        scoped ref Dragon4BigInteger divisor
    )
    {
        var divisorLength = divisor._length;
        if (dividend._length < divisorLength)
        {
            return 0;
        }

        var lastIndex = divisorLength - 1;
        var quotient = dividend._blocks[lastIndex] / (divisor._blocks[lastIndex] + 1);
        if (quotient != 0)
        {
            ulong borrow = 0;
            ulong carry = 0;
            for (var index = 0; index < divisorLength; index++)
            {
                var product = (ulong) divisor._blocks[index] * quotient + carry;
                carry = product >> BitsPerBlock;

                var difference = (ulong) dividend._blocks[index] - (uint) product - borrow;
                borrow = difference >> 63;
                dividend._blocks[index] = (uint) difference;
            }

            dividend.Trim();
        }

        if (Compare(ref dividend, ref divisor) >= 0)
        {
            quotient++;
            ulong borrow = 0;
            for (var index = 0; index < divisorLength; index++)
            {
                var difference = (ulong) dividend._blocks[index] - divisor._blocks[index] - borrow;
                borrow = difference >> 63;
                dividend._blocks[index] = (uint) difference;
            }

            dividend.Trim();
        }

        return quotient;
    }

    public void Multiply10()
    {
        Multiply(ref this, 10, out this);
    }

    public void MultiplyPow10(uint exponent)
    {
        if (exponent == 0 || IsZero)
        {
            return;
        }

        Pow10(exponent, out var power);
        Copy(out var value, ref this);
        Multiply(ref value, ref power, out this);
    }

    public void Multiply(scoped ref Dragon4BigInteger value)
    {
        Copy(out var left, ref this);
        Multiply(ref left, ref value, out this);
    }

    public void ShiftLeft(uint shift)
    {
        if (_length == 0 || shift == 0)
        {
            return;
        }

        var blocks = (int) (shift >> 5);
        var bits = (int) (shift & 31);
        Debug.Assert(_length + blocks + (bits == 0 ? 0 : 1) <= MaxBlockCount);

        if (blocks != 0)
        {
            for (var index = _length - 1; index >= 0; index--)
            {
                _blocks[index + blocks] = _blocks[index];
            }

            for (var index = 0; index < blocks; index++)
            {
                _blocks[index] = 0;
            }

            _length += blocks;
        }

        if (bits == 0)
        {
            return;
        }

        ulong carry = 0;
        for (var index = blocks; index < _length; index++)
        {
            var shifted = ((ulong) _blocks[index] << bits) | carry;
            _blocks[index] = (uint) shifted;
            carry = shifted >> BitsPerBlock;
        }

        if (carry != 0)
        {
            _blocks[_length++] = (uint) carry;
        }
    }

    private static void Copy(
        out Dragon4BigInteger result,
        scoped ref Dragon4BigInteger value
    )
    {
        result._length = value._length;
        for (var index = 0; index < value._length; index++)
        {
            result._blocks[index] = value._blocks[index];
        }
    }

    private void Trim()
    {
        while (_length > 0 && _blocks[_length - 1] == 0)
        {
            _length--;
        }
    }
}
