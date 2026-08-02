using System;
using System.Runtime.CompilerServices;

namespace Light.PortableResults.Validation;

/// <summary>
/// Provides extension methods for <see cref="Guid" />.
/// </summary>
public static class GuidExtensions
{
    /// <summary>
    /// Checks whether the specified GUID is a version 7 UUID as defined by RFC 9562 (section 5.7).
    /// </summary>
    /// <param name="value">The GUID to inspect.</param>
    /// <returns>
    /// <see langword="true" /> when the version field is <c>7</c> and the two most significant variant bits
    /// are <c>10</c> (the RFC 9562 variant); otherwise <see langword="false" />.
    /// </returns>
    /// <remarks>
    /// Both fields are checked: a value whose version nibble is <c>7</c> but that carries a non-RFC variant
    /// (for example the NCS-reserved variant) is not a UUIDv7 and cannot be produced by
    /// <c>Guid.CreateVersion7</c>. This method reads the GUID's storage directly and allocates nothing.
    /// </remarks>
    public static bool IsUuidV7(this Guid value)
    {
        ref var fields = ref Unsafe.As<Guid, GuidFields>(ref value);
        return ((fields.C >> 12) & 0x0F) == 7 && (fields.D & 0xC0) == 0x80;
    }

#pragma warning disable CS0649 // Fields are populated by reinterpreting Guid storage via Unsafe.As.
    private struct GuidFields
    {
        public int A;
        public short B;
        public short C; // the high nibble of the high byte carries the version
        public byte D; // the two most significant bits carry the variant
    }
#pragma warning restore CS0649
}
