# Runtime numerics provenance

The shortest floating-point digit generation in this folder is adapted from the
[dotnet/runtime](https://github.com/dotnet/runtime) repository's `release/6.0` servicing line,
using the immutable `v6.0.36` tag at commit
`f1dd57165bfd91875761329ac3a8b17f6606ad18`.

The following upstream files under
`src/libraries/System.Private.CoreLib/src/System` supplied the port:

- `Number.Grisu3.cs`
- `Number.DiyFp.cs`
- `Number.Dragon4.cs`
- `Number.BigInteger.cs`
- `Number.NumberBuffer.cs`
- `Number.Formatting.cs` (IEEE bit extraction and the floating-point-to-number dispatch)

Files containing adapted runtime code retain the .NET Foundation MIT header. The public
`CanonicalFloatingPointFormatter` and its invariant renderer are Light.PortableResults code.

## Adaptations

- Moved the implementation from the `System.Number` partial type into internal types in the
  `Light.PortableResults.Numbers` namespace.
- Retained only binary64 and binary32 shortest-round-trip formatting. Removed `Half`, parsing,
  counted Grisu3 formatting, and Dragon4's fixed significant-digit and fractional-digit cutoff modes.
- Replaced the pointer-backed upstream `NumberBuffer` with a small `Span<byte>`-backed ref struct
  containing only digits, digit count, and decimal scale.
- Replaced runtime-internal bit conversion and `BitOperations` dependencies with
  `FloatingPointBits` and `BitOperationsCompat`. The `net10.0` asset uses the framework leading-zero
  intrinsics; the `netstandard2.0` asset uses a semantically equivalent portable implementation.
- Retained Grisu3's cached powers and shortest-mode weed/rounding logic. Renamed `DiyFp` fields to
  properties and removed the unused counted-mode code and data.
- Specialized Dragon4 to shortest-unique mode. Algorithm choice is supplied per call by the
  formatter; no process-wide switch or mutable selection state exists.
- Reduced Dragon4's bignum to the operations reachable from shortest mode. Removed division,
  general conversions, fixed-digit support, and the upstream precomputed power-of-ten bignum table.
  Powers of ten are built with fixed-buffer exponentiation by squaring instead. The 128-block inline
  buffer is stack-only, requires `AllowUnsafeBlocks`, and leaves headroom above the binary64
  shortest-mode maximum.
- Replaced runtime-internal memory clearing and copying with bounded fixed-buffer loops.
- Added a direct invariant renderer for this library's notation thresholds, signed uppercase
  exponent form, negative zero, and positional whole-number `.0` marker.
- Made `TryFormat` the primary rendering path. `Format` uses a bounded stack buffer and constructs
  only the returned string.

The complete upstream license is reproduced in the repository-root `THIRD-PARTY-NOTICES.md`, which
is also packed at the root of the `Light.PortableResults` NuGet package.
