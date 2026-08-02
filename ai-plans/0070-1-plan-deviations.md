# Plan Deviations for Allocation-Free Canonical Formatting

## Referenced Plans

- `0058-fix-runtime-specific-number-metadata.md` introduced the public
  `CanonicalFloatingPointFormatter` for runtime-independent `double` and `float` text.
- `0061-add-utf-8-floating-point-formatting.md` added its UTF-8 overloads and established the
  private per-call Dragon4 test seam.
- `0070-0-try-format-canonical-zero-allocations.md` introduced `CanonicalTextFormatter` for the
  remaining primitive metadata kinds and explicitly retained the existing floating-point formatter.

## Deviations

### Unified formatter surface

The implemented API no longer exposes a separate `CanonicalFloatingPointFormatter`. Its public
constants and its `Format`, `TryFormat`, and `TryFormatUtf8` overloads for `double` and `float` are now
members of the partial `CanonicalTextFormatter` in `Light.PortableResults.Text`. The floating-point
surface resides in `CanonicalTextFormatter.FloatingPoint.cs`; the Grisu3, Dragon4, number-buffer, and
compatibility implementations remain internal types in `Light.PortableResults.Numbers`.

Production call sites, tests, and benchmarks use the unified formatter. The floating-point tests
continue to locate the two private generic `TryFormatCore` overloads for their forced-Dragon4 corpus,
so the test seam required by plans 0058 and 0061 is unchanged apart from its declaring type.

## Rationale

Once plan 0070 added canonical formatting for every other primitive kind, retaining a second public
formatter made the API harder to discover and forced consumers such as `MetadataValue` to dispatch
between two classes with the same destination, atomicity, and allocation contracts. A partial class
keeps the large floating-point implementation in its own source file without creating a runtime or
performance boundary. The library is not yet stable and permits breaking API changes, so consolidating
the surface now is preferable to preserving the historical split through forwarding APIs.

This deviation changes API ownership and source organization only. Floating-point text, exceptions,
capacity behavior, allocation behavior, UTF-8 output, algorithm selection, and wire formats remain
unchanged.

### Public code-unit helper

Plan 0070 originally required the shared code-unit helper to remain unexposed. The implemented
`CanonicalCodeUnit` is instead a public, top-level type in the focused `Light.PortableResults.Text`
namespace. This follows the repository's hide-in-plain-sight approach: advanced implementation-oriented
types remain accessible without adding them to the main namespace or nesting them inside a facade.

The type and its `FromAscii<TCodeUnit>` method are XML-documented. The public method supports
`byte` and `char`, matching the formatter's UTF-8 and UTF-16 destinations, and rejects other unmanaged
types with `NotSupportedException`. The supported generic instantiations retain their direct
allocation-free reinterpretation paths.
