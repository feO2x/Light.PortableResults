using System.Runtime.InteropServices;

namespace Light.PortableResults.Metadata;

/// <summary>
/// <para>
/// Internal storage for <see cref="MetadataValue" />. Uses explicit layout to minimize memory:
/// </para>
/// <para>
/// - <see cref="Int64" /> and <see cref="Float64" /> share offset 0 (union-style, 8 bytes). Only one is
/// meaningful at a time, determined by <see cref="MetadataKind" />.
/// </para>
/// <para>
/// - <see cref="Reference" /> is at offset 8 to avoid overlapping with the primitives. This separation
/// is critical: the .NET GC tracks object references, and if Ref overlapped with I64/F64, the GC
/// could misinterpret a raw integer as a pointer, causing crashes or heap corruption.
/// </para>
/// <para>
/// Total struct size: 16 bytes (8 for primitives + 8 for reference on 64-bit systems).
/// </para>
/// </summary>
[StructLayout(LayoutKind.Explicit)]
internal readonly record struct MetadataPayload
{
    public MetadataPayload(long int64) => Int64 = int64;

    public MetadataPayload(double float64) => Float64 = float64;

    public MetadataPayload(object? reference) => Reference = reference;

    // Int64 and Float64 overlap at offset 0 - only one is valid based on MetadataKind
    [field: FieldOffset(0)]
    public long Int64 { get; }

    [field: FieldOffset(0)]
    public double Float64 { get; }

    // Ref is at offset 8 (after the 8-byte primitives) to keep it separate from Int64/Float64.
    // This prevents the GC from misinterpreting primitive values as object references.
    [field: FieldOffset(8)]
    public object? Reference { get; }
}
