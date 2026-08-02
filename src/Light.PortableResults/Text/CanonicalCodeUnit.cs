using System;
using System.Runtime.CompilerServices;

namespace Light.PortableResults.Text;

/// <summary>
/// Converts ASCII bytes to the UTF-8 or UTF-16 code units used by canonical text formatters.
/// </summary>
/// <remarks>
/// This low-level helper lets one generic renderer target either <see cref="byte" /> or
/// <see cref="char" /> destinations. Other code-unit types are not supported.
/// </remarks>
public static class CanonicalCodeUnit
{
    /// <summary>Converts an ASCII byte to a UTF-8 byte or UTF-16 character code unit.</summary>
    /// <typeparam name="TCodeUnit">The destination code-unit type, either <see cref="byte" /> or <see cref="char" />.</typeparam>
    /// <param name="value">The ASCII byte to convert.</param>
    /// <returns>The equivalent code unit.</returns>
    /// <exception cref="NotSupportedException">
    /// <typeparamref name="TCodeUnit" /> is neither <see cref="byte" /> nor <see cref="char" />.
    /// </exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static TCodeUnit FromAscii<TCodeUnit>(byte value)
        where TCodeUnit : unmanaged
    {
        if (typeof(TCodeUnit) == typeof(byte))
        {
            return Unsafe.As<byte, TCodeUnit>(ref value);
        }

        if (typeof(TCodeUnit) == typeof(char))
        {
            var character = (char) value;
            return Unsafe.As<char, TCodeUnit>(ref character);
        }

        throw new NotSupportedException("Only byte and char code units are supported.");
    }
}
