// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#if NET10_0_OR_GREATER
using System.Numerics;
#endif

namespace Light.PortableResults.Numbers;

internal static class BitOperationsCompat
{
    public static int LeadingZeroCount(uint value)
    {
#if NET10_0_OR_GREATER
        return BitOperations.LeadingZeroCount(value);
#else
        if (value == 0)
        {
            return 32;
        }

        var count = 0;
        if ((value & 0xFFFF0000U) == 0)
        {
            count += 16;
            value <<= 16;
        }

        if ((value & 0xFF000000U) == 0)
        {
            count += 8;
            value <<= 8;
        }

        if ((value & 0xF0000000U) == 0)
        {
            count += 4;
            value <<= 4;
        }

        if ((value & 0xC0000000U) == 0)
        {
            count += 2;
            value <<= 2;
        }

        return count + ((value & 0x80000000U) == 0 ? 1 : 0);
#endif
    }

    public static int LeadingZeroCount(ulong value)
    {
#if NET10_0_OR_GREATER
        return BitOperations.LeadingZeroCount(value);
#else
        var high = (uint) (value >> 32);
        return high == 0 ? 32 + LeadingZeroCount((uint) value) : LeadingZeroCount(high);
#endif
    }

    public static int Log2(uint value) => 31 - LeadingZeroCount(value);

    public static int Log2(ulong value) => 63 - LeadingZeroCount(value);
}
