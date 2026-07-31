// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace Light.PortableResults.Numbers;

internal ref struct NumberBuffer
{
    public NumberBuffer(Span<byte> digits)
    {
        Digits = digits;
        DigitsCount = 0;
        Scale = 0;
    }

    public Span<byte> Digits { get; }

    public int DigitsCount { get; set; }

    public int Scale { get; set; }
}
